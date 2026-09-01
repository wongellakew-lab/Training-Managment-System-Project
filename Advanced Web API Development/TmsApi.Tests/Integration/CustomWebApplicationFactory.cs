using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public void SeedData()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
        db.Database.EnsureCreated();

        if (!db.Courses.Any(c => c.Code == "CS101"))
        {
            db.Courses.Add(new Course
            {
                Code = "CS101",
                Title = "Introduction to Computer Science",
                MaxCapacity = 30
            });

            db.Students.Add(new Student
            {
                RegistrationNumber = "REG-TEST-001",
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice.smith@test.local",
                Gpa = 3.8m,
                EnrollmentDate = DateTime.UtcNow
            });

            db.SaveChanges();
        }
    }
}
