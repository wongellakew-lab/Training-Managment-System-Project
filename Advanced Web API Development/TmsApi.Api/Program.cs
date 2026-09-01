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

// Seed initial roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TmsUser>>();
    var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    db.Database.EnsureCreated();

    string[] roles = ["Admin", "Instructor", "Student"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
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
    var instUserEmail = "instructor@tms.local";
    if (await userManager.FindByEmailAsync(instUserEmail) == null)
    {
        var instructor = new Instructor
        {
            FirstName = "Alan",
            LastName = "Turing",
            Email = instUserEmail,
            EmployeeNumber = "EMP-001",
            Department = "Computer Science",
            HireDate = DateTime.UtcNow.AddYears(-3)
        };
        await db.Instructors.AddAsync(instructor);
        await db.SaveChangesAsync();

        var instUser = new TmsUser
        {
            UserName = instUserEmail,
            Email = instUserEmail,
            FirstName = "Alan",
            LastName = "Turing",
            InstructorId = instructor.Id,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(instUser, "Instructor123!");
        await userManager.AddToRoleAsync(instUser, "Instructor");
    }

    // Seed Student
    var studentUserEmail = "student@tms.local";
    TmsUser? studentUser = await userManager.FindByEmailAsync(studentUserEmail);
    Student? student = null;
    if (studentUser == null)
    {
        student = new Student
        {
            FirstName = "Alice",
            LastName = "Walker",
            Email = studentUserEmail,
            RegistrationNumber = "REG-2026-001",
            Gpa = 3.85m
        };
        await db.Students.AddAsync(student);
        await db.SaveChangesAsync();

        studentUser = new TmsUser
        {
            UserName = studentUserEmail,
            Email = studentUserEmail,
            FirstName = "Alice",
            LastName = "Walker",
            StudentId = student.Id,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(studentUser, "Student123!");
        await userManager.AddToRoleAsync(studentUser, "Student");
    }

    // Seed Courses
    if (!await db.Courses.AnyAsync())
    {
        var instructor = await db.Instructors.FirstOrDefaultAsync();
        var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Introduction to Computer Science", Description = "Fundamental principles of programming and algorithmic problem solving.", Department = "Computer Science", Credits = 3, MaxCapacity = 30, InstructorId = instructor?.Id },
            new() { Code = "CS-201", Title = "Data Structures & Algorithms", Description = "In-depth exploration of arrays, linked lists, trees, graphs, and search algorithms.", Department = "Computer Science", Credits = 4, MaxCapacity = 25, InstructorId = instructor?.Id },
            new() { Code = "CS-301", Title = "Full-Stack Web Architecture", Description = "Modern scalable web applications using .NET, Angular, RESTful APIs, and SignalR.", Department = "Software Engineering", Credits = 4, MaxCapacity = 20, InstructorId = instructor?.Id },
            new() { Code = "CS-401", Title = "Cloud Native Computing & DevOps", Description = "Microservices, containerization with Docker/Kubernetes, and CI/CD pipelines.", Department = "Cloud Computing", Credits = 3, MaxCapacity = 25, InstructorId = instructor?.Id },
            new() { Code = "AI-501", Title = "Applied Machine Learning & Neural Networks", Description = "Supervised/unsupervised learning, deep learning with PyTorch, and generative models.", Department = "Artificial Intelligence", Credits = 4, MaxCapacity = 18, InstructorId = instructor?.Id }
        };
        await db.Courses.AddRangeAsync(courses);
        await db.SaveChangesAsync();

        if (student != null)
        {
            var enrollments = new List<Enrollment>
            {
                new() { CourseId = courses[0].Id, StudentId = student.Id, Status = EnrollmentStatus.Completed, Grade = 3.9m, EnrolledAt = DateTime.UtcNow.AddMonths(-3) },
                new() { CourseId = courses[1].Id, StudentId = student.Id, Status = EnrollmentStatus.Completed, Grade = 3.8m, EnrolledAt = DateTime.UtcNow.AddMonths(-2) },
                new() { CourseId = courses[2].Id, StudentId = student.Id, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow.AddDays(-10) },
                new() { CourseId = courses[3].Id, StudentId = student.Id, Status = EnrollmentStatus.Pending, EnrolledAt = DateTime.UtcNow.AddDays(-2) }
            };
            await db.Enrollments.AddRangeAsync(enrollments);

            var cert = new Certificate
            {
                StudentId = student.Id,
                CourseId = courses[0].Id,
                CertificateNumber = $"CERT-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                IssueDate = DateTime.UtcNow.AddMonths(-1)
            };
            await db.Certificates.AddAsync(cert);

            var notifications = new List<Notification>
            {
                new() { UserId = studentUser.Id, Title = "Welcome to TMS", Message = "Welcome to the Training Management System! Explore available courses.", Type = "System", IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new() { UserId = studentUser.Id, Title = "Enrollment Submitted", Message = "Your enrollment request for CS-401 has been submitted.", Type = "Enrollment", IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
            };
            await db.Notifications.AddRangeAsync(notifications);
            await db.SaveChangesAsync();
        }
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
