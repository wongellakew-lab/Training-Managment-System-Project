using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using TmsApi.Api.Authorization;
using TmsApi.Api.Diagnostics;
using TmsApi.Api.Extensions;
using TmsApi.Api.Middleware;
using TmsApi.Application.Behaviors;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Grading;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Mappings;
using TmsApi.Application.Shaping;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Caching;
using TmsApi.Infrastructure.ExternalServices;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Workers;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Database Context
builder.Services.AddDbContext<TmsDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("TmsTestDb");
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    }
});

// Identity
builder.Services.AddIdentity<TmsUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<TmsDbContext>()
.AddDefaultTokenProviders();

// Authentication & JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_dev_key_which_is_at_least_32_bytes_long_12345";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TmsApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TmsClients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MinimumGpa", policy =>
        policy.Requirements.Add(new MinimumGpaRequirement(3.0m)));
});
builder.Services.AddScoped<IAuthorizationHandler, MinimumGpaHandler>();

// HybridCache
#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

// MediatR & Pipeline Behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(MappingProfile).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Application & Infrastructure Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<GradingService>();
builder.Services.AddScoped<DataShaper<CourseDto>>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<ICourseService>(sp => sp.GetRequiredService<CourseService>());
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// SignalR & Background Channels
builder.Services.AddSignalR();
builder.Services.AddSingleton<TranscriptQueue>();
builder.Services.AddHostedService<TranscriptWorker>();

// Custom Versioning
builder.Services.AddCustomVersioning();

// OpenTelemetry Diagnostics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(Diagnostics.ServiceName))
    .WithTracing(tracing =>
    {
        tracing.AddSource(Diagnostics.ServiceName)
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(Diagnostics.ServiceName)
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddConsoleExporter();
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed initial roles, users, courses, schedules, and enrollments
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TmsUser>>();
    var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (db.Database.IsRelational())
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Schedules"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""Title"" character varying(200) NOT NULL,
                    ""Category"" character varying(50) NOT NULL,
                    ""Date"" timestamp with time zone NOT NULL,
                    ""StartTime"" character varying(50) NOT NULL,
                    ""EndTime"" character varying(50) NOT NULL,
                    ""PicName"" character varying(100) NOT NULL,
                    ""Location"" character varying(200) NOT NULL,
                    ""Notes"" text,
                    ""AttendeesCount"" integer NOT NULL DEFAULT 0,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" timestamp with time zone,
                    CONSTRAINT ""PK_Schedules"" PRIMARY KEY (""Id"")
                );

                ALTER TABLE ""Schedules"" ADD COLUMN IF NOT EXISTS ""PicAvatar"" character varying(500) NOT NULL DEFAULT '';
                ALTER TABLE ""Schedules"" ADD COLUMN IF NOT EXISTS ""AttendeesCount"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""Schedules"" ADD COLUMN IF NOT EXISTS ""IsDeleted"" boolean NOT NULL DEFAULT false;

                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""Category"" character varying(100) NOT NULL DEFAULT 'General';
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""Level"" character varying(50) NOT NULL DEFAULT 'Beginner';
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""Price"" numeric NOT NULL DEFAULT 0.0;
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""Status"" character varying(50) NOT NULL DEFAULT 'Active';
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""DurationHours"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""LessonsCount"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""Rating"" numeric NOT NULL DEFAULT 5.0;
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""ReviewsCount"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""ImageUrl"" text NOT NULL DEFAULT '';
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""About"" text NOT NULL DEFAULT '';
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""FeaturesJson"" text NOT NULL DEFAULT '[]';
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""ResourcesJson"" text NOT NULL DEFAULT '[]';
                ALTER TABLE ""Courses"" ADD COLUMN IF NOT EXISTS ""EnrolledStudentsCount"" integer NOT NULL DEFAULT 0;

                ALTER TABLE ""Enrollments"" ADD COLUMN IF NOT EXISTS ""PhoneNumber"" character varying(50);
                ALTER TABLE ""Enrollments"" ADD COLUMN IF NOT EXISTS ""Category"" character varying(100);
                ALTER TABLE ""Enrollments"" ADD COLUMN IF NOT EXISTS ""Batch"" character varying(50);
                ALTER TABLE ""Enrollments"" ADD COLUMN IF NOT EXISTS ""Fee"" numeric NOT NULL DEFAULT 0.0;
                ALTER TABLE ""Enrollments"" ADD COLUMN IF NOT EXISTS ""PaymentStatus"" character varying(50) NOT NULL DEFAULT 'Pending';
                ALTER TABLE ""Enrollments"" ADD COLUMN IF NOT EXISTS ""Country"" character varying(100);
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] DB Schema sync: {ex.Message}");
        }
    }

    string[] roles = ["Admin", "Instructor", "Student"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Seed Noah Hansen (Admin depicted in mockups)
    var noahEmail = "noah.hansen@eduva.com";
    if (await userManager.FindByEmailAsync(noahEmail) == null)
    {
        var noah = new TmsUser
        {
            UserName = noahEmail,
            Email = noahEmail,
            FirstName = "Noah",
            LastName = "Hansen",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(noah, "Admin123!");
        await userManager.AddToRoleAsync(noah, "Admin");
    }

    var adminEmail = "admin@tms.local";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new TmsUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Admin",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, "Admin123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    // Seed Instructors
    var instructors = new List<Instructor>
    {
        new() { FirstName = "Gina", LastName = "Alexander", Email = "gina.alexander@eduva.com", EmployeeNumber = "EMP-001", Department = "Web Dev", HireDate = DateTime.UtcNow.AddYears(-4) },
        new() { FirstName = "Elise", LastName = "Wong", Email = "elise.wong@eduva.com", EmployeeNumber = "EMP-002", Department = "Design", HireDate = DateTime.UtcNow.AddYears(-3) },
        new() { FirstName = "Fabricio", LastName = "Conrad", Email = "fabricio.conrad@eduva.com", EmployeeNumber = "EMP-003", Department = "Marketing", HireDate = DateTime.UtcNow.AddYears(-2) },
        new() { FirstName = "Angela", LastName = "Cole", Email = "angela.cole@eduva.com", EmployeeNumber = "EMP-004", Department = "Communication", HireDate = DateTime.UtcNow.AddYears(-3) },
        new() { FirstName = "Mason", LastName = "James", Email = "mason.james@eduva.com", EmployeeNumber = "EMP-005", Department = "Web Dev", HireDate = DateTime.UtcNow.AddYears(-1) },
        new() { FirstName = "Jacqueline", LastName = "Hart", Email = "jacqueline.hart@eduva.com", EmployeeNumber = "EMP-006", Department = "Design", HireDate = DateTime.UtcNow.AddYears(-2) }
    };

    if (!await db.Instructors.AnyAsync())
    {
        await db.Instructors.AddRangeAsync(instructors);
        await db.SaveChangesAsync();

        foreach (var inst in instructors)
        {
            var instUser = new TmsUser
            {
                UserName = inst.Email,
                Email = inst.Email,
                FirstName = inst.FirstName,
                LastName = inst.LastName,
                InstructorId = inst.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(instUser, "Instructor123!");
            await userManager.AddToRoleAsync(instUser, "Instructor");
        }
    }

    // Seed Students
    var students = new List<Student>
    {
        new() { FirstName = "Grasiella", LastName = "Lopez", Email = "grasiella.lopez@eduva.com", RegistrationNumber = "REG-2030-001", Gpa = 3.9m },
        new() { FirstName = "Hendrick", LastName = "Chow", Email = "hendrick.chow@eduva.com", RegistrationNumber = "REG-2030-002", Gpa = 3.7m },
        new() { FirstName = "Jenice", LastName = "Miller", Email = "jenice.miller@eduva.com", RegistrationNumber = "REG-2030-003", Gpa = 3.8m },
        new() { FirstName = "Nigel", LastName = "Bruce", Email = "nigel.bruce@eduva.com", RegistrationNumber = "REG-2030-004", Gpa = 3.5m },
        new() { FirstName = "Amber", LastName = "Lunge", Email = "amber.lunge@eduva.com", RegistrationNumber = "REG-2030-005", Gpa = 3.95m },
        new() { FirstName = "L. Mitch", LastName = "Saul", Email = "mitch.saul@eduva.com", RegistrationNumber = "REG-2030-006", Gpa = 3.6m },
        new() { FirstName = "Ingrid", LastName = "White", Email = "ingrid.white@eduva.com", RegistrationNumber = "REG-2030-007", Gpa = 3.85m },
        new() { FirstName = "Jacob", LastName = "Palle", Email = "jacob.palle@eduva.com", RegistrationNumber = "REG-2030-008", Gpa = 3.4m }
    };

    if (!await db.Students.AnyAsync())
    {
        await db.Students.AddRangeAsync(students);
        await db.SaveChangesAsync();

        foreach (var st in students)
        {
            var stUser = new TmsUser
            {
                UserName = st.Email,
                Email = st.Email,
                FirstName = st.FirstName,
                LastName = st.LastName,
                StudentId = st.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(stUser, "Student123!");
            await userManager.AddToRoleAsync(stUser, "Student");
        }
    }

    // Seed Courses
    if (!await db.Courses.AnyAsync())
    {
        var instList = await db.Instructors.ToListAsync();
        var courses = new List<Course>
        {
            new()
            {
                Code = "DES-101",
                Title = "Graphic Design Fundamentals",
                Category = "Design",
                Price = 99m,
                Status = "Active",
                Level = "Beginner",
                DurationHours = 40,
                LessonsCount = 20,
                Rating = 4.8m,
                ReviewsCount = 275,
                EnrolledStudentsCount = 320,
                Description = "Master the core principles of visual design, color theory, typography, and vector illustration.",
                About = "This course covers modern graphic design methodologies, creative brand identity development, and practical portfolio creation.",
                ImageUrl = "https://images.unsplash.com/photo-1626785774573-4b799315345d?w=600&auto=format&fit=crop&q=80",
                InstructorId = instList.FirstOrDefault(i => i.FirstName == "Elise")?.Id
            },
            new()
            {
                Code = "MKT-201",
                Title = "Digital Marketing Mastery",
                Category = "Marketing",
                Price = 79m,
                Status = "Active",
                Level = "Intermediate",
                DurationHours = 30,
                LessonsCount = 18,
                Rating = 4.7m,
                ReviewsCount = 280,
                EnrolledStudentsCount = 280,
                Description = "Accelerate digital growth through data-driven performance marketing, SEO, and paid ad strategies.",
                About = "Learn comprehensive performance marketing, multi-channel funnels, analytics tracking, and retention campaigns.",
                ImageUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=600&auto=format&fit=crop&q=80",
                InstructorId = instList.FirstOrDefault(i => i.FirstName == "Fabricio")?.Id
            },
            new()
            {
                Code = "WEB-101",
                Title = "Python for Beginners",
                Category = "Web Dev",
                Price = 89m,
                Status = "Active",
                Level = "Beginner",
                DurationHours = 50,
                LessonsCount = 25,
                Rating = 4.9m,
                ReviewsCount = 350,
                EnrolledStudentsCount = 350,
                Description = "The course focuses on practical coding exercises, problem-solving techniques, and beginner-friendly projects to help students build confidence in writing and understanding code for real-world applications.",
                About = "The course focuses on practical coding exercises, problem-solving techniques, and beginner-friendly projects to help students build confidence in writing and understanding code for real-world applications.",
                ImageUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=600&auto=format&fit=crop&q=80",
                InstructorId = instList.FirstOrDefault(i => i.FirstName == "Gina")?.Id
            },
            new()
            {
                Code = "LAN-101",
                Title = "French for Beginners",
                Category = "Language",
                Price = 69m,
                Status = "Draft",
                Level = "Beginner",
                DurationHours = 25,
                LessonsCount = 15,
                Rating = 4.5m,
                ReviewsCount = 120,
                EnrolledStudentsCount = 0,
                Description = "Introduction to everyday conversational French grammar, pronunciation, and vocabulary.",
                About = "An engaging foundation course in European French, cultural contexts, and everyday dialogues.",
                ImageUrl = "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=600&auto=format&fit=crop&q=80",
                InstructorId = instList.FirstOrDefault()?.Id
            },
            new()
            {
                Code = "COM-101",
                Title = "Public Speaking & Presentation Skills",
                Category = "Communication",
                Price = 69m,
                Status = "Active",
                Level = "Beginner",
                DurationHours = 28,
                LessonsCount = 16,
                Rating = 4.6m,
                ReviewsCount = 210,
                EnrolledStudentsCount = 210,
                Description = "Deliver compelling presentations with executive presence, storytelling, and vocal control.",
                About = "Master the art of high-impact presentations, stage confidence, body language, and slide design.",
                ImageUrl = "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=600&auto=format&fit=crop&q=80",
                InstructorId = instList.FirstOrDefault(i => i.FirstName == "Angela")?.Id
            },
            new()
            {
                Code = "MKT-301",
                Title = "Social Media Strategies",
                Category = "Marketing",
                Price = 109m,
                Status = "Archived",
                Level = "Advanced",
                DurationHours = 40,
                LessonsCount = 24,
                Rating = 4.4m,
                ReviewsCount = 190,
                EnrolledStudentsCount = 0,
                Description = "Advanced brand storytelling, viral short-form video creation, and influencer collaborations.",
                About = "Strategic playbook for building high-engagement social media followings and revenue-generating brands.",
                ImageUrl = "https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=600&auto=format&fit=crop&q=80",
                InstructorId = instList.FirstOrDefault(i => i.FirstName == "Fabricio")?.Id
            },
            new()
            {
                Code = "DES-201",
                Title = "UX/UI Design Fundamentals",
                Category = "Design",
                Price = 89m,
                Status = "Active",
                Level = "Beginner",
                DurationHours = 35,
                LessonsCount = 20,
                Rating = 4.9m,
                ReviewsCount = 298,
                EnrolledStudentsCount = 290,
                Description = "Create user-centered design systems, responsive wireframes, and interactive Figma prototypes.",
                About = "End-to-end UX research techniques, user journeys, wireframing, component libraries, and testing in Figma.",
                ImageUrl = "https://images.unsplash.com/photo-1581291518857-4e27b48ff24e?w=600&auto=format&fit=crop&q=80",
                InstructorId = instList.FirstOrDefault(i => i.FirstName == "Jacqueline")?.Id
            },
            new()
            {
                Code = "WEB-301",
                Title = "Full Stack Web Development Bootcamp",
                Category = "Web Dev",
                Price = 99m,
                Status = "Active",
                Level = "Intermediate",
                DurationHours = 60,
                LessonsCount = 28,
                Rating = 4.8m,
                ReviewsCount = 260,
                EnrolledStudentsCount = 260,
                Description = "Build scalable, production-ready web apps with Angular, .NET Web API, EF Core, and PostgreSQL.",
                About = "Hands-on project-based engineering path covering full stack REST, microservices, auth, state management, and cloud.",
                ImageUrl = "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=600&auto=format&fit=crop&q=80",
                InstructorId = instList.FirstOrDefault(i => i.FirstName == "Mason")?.Id
            }
        };
        await db.Courses.AddRangeAsync(courses);
        await db.SaveChangesAsync();
    }

    // Seed Schedules (May 2030)
    if (!await db.Schedules.AnyAsync())
    {
        var schedules = new List<Schedule>
        {
            new()
            {
                Title = "Monthly Mentor Alignment",
                Category = "Meetings",
                Date = new DateTime(2030, 5, 14, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "09:00 AM",
                EndTime = "10:30 AM",
                PicName = "James Ocampo",
                Location = "Eduva Executive Boardroom",
                Notes = "Monthly alignment session with mentor team lead to discuss learning roadmaps.",
                AttendeesCount = 8
            },
            new()
            {
                Title = "Creative Portfolio Review Session",
                Category = "Workshops",
                Date = new DateTime(2030, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "09:30 AM",
                EndTime = "12:00 PM",
                PicName = "James Ocampo",
                Location = "Eduva Creative Studio Room",
                Notes = "Participants are encouraged to bring their latest portfolio projects for live feedback and discussion.",
                AttendeesCount = 12
            },
            new()
            {
                Title = "Eduva Community Networking Night",
                Category = "Events",
                Date = new DateTime(2030, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "06:30 PM",
                EndTime = "08:30 PM",
                PicName = "Marcus Delgado",
                Location = "Eduva Main Hall & Lounge Area",
                Notes = "Casual networking session with mentors, students, and guest speakers. Snacks and refreshments provided.",
                AttendeesCount = 9
            },
            new()
            {
                Title = "Course Planning Discussion",
                Category = "Meetings",
                Date = new DateTime(2030, 5, 17, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "10:00 AM",
                EndTime = "11:00 AM",
                PicName = "Elise Wong",
                Location = "Eduva Conference Room B",
                Notes = "Quarterly curriculum review and scheduling of Q3 design cohorts.",
                AttendeesCount = 6
            },
            new()
            {
                Title = "Frontend Development Bootcamp",
                Category = "Workshops",
                Date = new DateTime(2030, 5, 18, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "02:00 PM",
                EndTime = "05:00 PM",
                PicName = "Mason James",
                Location = "Tech Lab 2 & Live Stream",
                Notes = "Deep dive into reactive state patterns with Angular Signals and NgRx.",
                AttendeesCount = 24
            },
            new()
            {
                Title = "AI Productivity Seminar",
                Category = "Events",
                Date = new DateTime(2030, 5, 18, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "07:00 PM",
                EndTime = "09:00 PM",
                PicName = "Alan Turing",
                Location = "Eduva Main Auditorium",
                Notes = "Modern AI assisted tooling and developer productivity workflows.",
                AttendeesCount = 45
            },
            new()
            {
                Title = "Weekly Mentor Coordination",
                Category = "Meetings",
                Date = new DateTime(2030, 5, 21, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "09:00 AM",
                EndTime = "10:00 AM",
                PicName = "Gina Alexander",
                Location = "Meeting Room 3",
                Notes = "Weekly standup to address student blocker tickets and review lab assessments.",
                AttendeesCount = 7
            },
            new()
            {
                Title = "Student Success and Career Webinar",
                Category = "Workshops",
                Date = new DateTime(2030, 5, 21, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "01:00 PM",
                EndTime = "03:00 PM",
                PicName = "Angela Cole",
                Location = "Eduva Studio 1",
                Notes = "Resume writing, technical interview readiness, and portfolio presentation skills.",
                AttendeesCount = 32
            },
            new()
            {
                Title = "Public Speaking Essentials Workshop",
                Category = "Workshops",
                Date = new DateTime(2030, 5, 24, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "09:30 AM",
                EndTime = "12:00 PM",
                PicName = "Angela Cole",
                Location = "Eduva Creative Studio Room",
                Notes = "Overcoming stage anxiety and delivering persuasive project pitches.",
                AttendeesCount = 18
            },
            new()
            {
                Title = "Learning Community Meetup",
                Category = "Events",
                Date = new DateTime(2030, 5, 24, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "06:30 PM",
                EndTime = "08:00 PM",
                PicName = "Marcus Delgado",
                Location = "Eduva Lounge & Coffee Bar",
                Notes = "Connect with fellow cohort peers and exchange project ideas.",
                AttendeesCount = 20
            },
            new()
            {
                Title = "Monthly Instructor Briefing",
                Category = "Meetings",
                Date = new DateTime(2030, 5, 27, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "09:00 AM",
                EndTime = "10:30 AM",
                PicName = "Fabricio Conrad",
                Location = "Executive Briefing Room",
                Notes = "Review of monthly grading performance and instructor teaching hours.",
                AttendeesCount = 10
            },
            new()
            {
                Title = "Eduva Student Appreciation Day",
                Category = "Events",
                Date = new DateTime(2030, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                StartTime = "05:00 PM",
                EndTime = "08:00 PM",
                PicName = "Noah Hansen",
                Location = "Eduva Campus Courtyard",
                Notes = "Celebration dinner honoring outstanding student achievements and certificate recipients.",
                AttendeesCount = 60
            }
        };
        await db.Schedules.AddRangeAsync(schedules);
        await db.SaveChangesAsync();
    }

    // Seed Enrollments
    if (!await db.Enrollments.AnyAsync())
    {
        var stList = await db.Students.ToListAsync();
        var crList = await db.Courses.ToListAsync();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = stList[0].Id, CourseId = crList[6].Id, Status = EnrollmentStatus.Active, Batch = "Batch 12", Fee = 42m, PaymentStatus = "Paid", PhoneNumber = "+1 202-555-0142", Country = "USA", EnrolledAt = new DateTime(2030, 5, 18, 0, 0, 0, DateTimeKind.Utc) },
            new() { StudentId = stList[1].Id, CourseId = crList[2].Id, Status = EnrollmentStatus.Pending, Batch = "Batch 8", Fee = 55m, PaymentStatus = "Pending", PhoneNumber = "+1 202-555-0189", Country = "Canada", EnrolledAt = new DateTime(2030, 5, 18, 0, 0, 0, DateTimeKind.Utc) },
            new() { StudentId = stList[2].Id, CourseId = crList[4].Id, Status = EnrollmentStatus.Active, Batch = "Batch 5", Fee = 27m, PaymentStatus = "Paid", PhoneNumber = "+1 202-555-0171", Country = "UK", EnrolledAt = new DateTime(2030, 5, 17, 0, 0, 0, DateTimeKind.Utc) },
            new() { StudentId = stList[3].Id, CourseId = crList[1].Id, Status = EnrollmentStatus.Waitlisted, Batch = "Batch 10", Fee = 49m, PaymentStatus = "Unpaid", PhoneNumber = "+1 202-555-0133", Country = "Australia", EnrolledAt = new DateTime(2030, 5, 17, 0, 0, 0, DateTimeKind.Utc) },
            new() { StudentId = stList[4].Id, CourseId = crList[0].Id, Status = EnrollmentStatus.Active, Batch = "Batch 6", Fee = 32m, PaymentStatus = "Paid", PhoneNumber = "+1 202-555-0160", Country = "Germany", EnrolledAt = new DateTime(2030, 5, 16, 0, 0, 0, DateTimeKind.Utc) },
            new() { StudentId = stList[5].Id, CourseId = crList[7].Id, Status = EnrollmentStatus.Pending, Batch = "Batch 4", Fee = 79m, PaymentStatus = "Pending", PhoneNumber = "+1 202-555-0194", Country = "USA", EnrolledAt = new DateTime(2030, 5, 16, 0, 0, 0, DateTimeKind.Utc) },
            new() { StudentId = stList[6].Id, CourseId = crList[0].Id, Status = EnrollmentStatus.Active, Batch = "Batch 9", Fee = 39m, PaymentStatus = "Paid", PhoneNumber = "+1 202-555-0118", Country = "USA", EnrolledAt = new DateTime(2030, 5, 15, 0, 0, 0, DateTimeKind.Utc) },
            new() { StudentId = stList[7].Id, CourseId = crList[1].Id, Status = EnrollmentStatus.Waitlisted, Batch = "Batch 7", Fee = 51m, PaymentStatus = "Unpaid", PhoneNumber = "+1 202-555-0125", Country = "UK", EnrolledAt = new DateTime(2030, 5, 15, 0, 0, 0, DateTimeKind.Utc) }
        };

        await db.Enrollments.AddRangeAsync(enrollments);
        await db.SaveChangesAsync();
    }
}

// Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TmsHub>("/hubs/tms");

app.Run();

public partial class Program { }
