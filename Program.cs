using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite database
builder.Services.AddDbContext<EnrollmentDbContext>(options =>
{
    options.UseSqlite("Data Source=test.db"); // SQLite connection string
});

var app = builder.Build();

// Ensure the database is created and apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
    dbContext.Database.EnsureCreated();
    dbContext.Database.Migrate(); // Apply pending migrations
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve the index.html file
app.UseStaticFiles();

app.MapGet("/", (HttpContext context) =>
{
    return context.Response.WriteAsync("<!DOCTYPE html>\n" +
                                        "<html>\n" +
                                        "<head>\n" +
                                        "    <title>Enrollment Form</title>\n" +
                                        "</head>\n" +
                                        "<body>\n" +
                                        "    <h1>Add New Enrollment</h1>\n" +
                                        "    <form action=\"/enrollments\" method=\"post\">\n" +
                                        "        <label for=\"studentId\">Student ID:</label>\n" +
                                        "        <input type=\"number\" id=\"studentId\" name=\"StudentId\" required><br><br>\n" +
                                        "        <label for=\"studentName\">Student Name:</label>\n" +
                                        "        <input type=\"text\" id=\"studentName\" name=\"StudentName\" required><br><br>\n" +
                                        "        <label for=\"dateOfBirth\">Date of Birth:</label>\n" +
                                        "        <input type=\"date\" id=\"dateOfBirth\" name=\"DateOfBirth\" required><br><br>\n" +
                                        "        <label for=\"gender\">Gender:</label>\n" +
                                        "        <select id=\"gender\" name=\"Gender\">\n" +
                                        "            <option value=\"Male\">Male</option>\n" +
                                        "            <option value=\"Female\">Female</option>\n" +
                                        "            <option value=\"Other\">Other</option>\n" +
                                        "        </select><br><br>\n" +
                                        "        <label for=\"address\">Address:</label>\n" +
                                        "        <textarea id=\"address\" name=\"Address\" required></textarea><br><br>\n" +
                                        "        <label for=\"email\">Email:</label>\n" +
                                        "        <input type=\"email\" id=\"email\" name=\"Email\" required><br><br>\n" +
                                        "        <label for=\"phoneNumber\">Phone Number:</label>\n" +
                                        "        <input type=\"tel\" id=\"phoneNumber\" name=\"PhoneNumber\" required><br><br>\n" +
                                        "        <label for=\"courseId\">Course ID:</label>\n" +
                                        "        <input type=\"number\" id=\"courseId\" name=\"CourseId\" required><br><br>\n" +
                                        "        <label for=\"enrollmentDate\">Enrollment Date:</label>\n" +
                                        "        <input type=\"date\" id=\"enrollmentDate\" name=\"EnrollmentDate\" required><br><br>\n" +
                                        "        <label for=\"registrationDate\">Registration Date:</label>\n" +
                                        "        <input type=\"date\" id=\"registrationDate\" name=\"RegistrationDate\" required><br><br>\n" +
                                        "        <button type=\"submit\">Submit</button>\n" +
                                        "    </form>\n" +
                                        "</body>\n" +
                                        "</html>");
});

app.MapPost("/enrollments", async (HttpContext context) =>
{
    // Parse form data
    var form = await context.Request.ReadFormAsync();
    if (!int.TryParse(form["StudentId"], out var studentId) ||
        !int.TryParse(form["CourseId"], out var courseId) ||
        !DateTime.TryParse(form["EnrollmentDate"], out var enrollmentDate) ||
        !DateTime.TryParse(form["RegistrationDate"], out var registrationDate) ||
        !DateTime.TryParse(form["DateOfBirth"], out var dateOfBirth))
    {
        context.Response.StatusCode = 400; // Bad request
        return;
    }

    // Add enrollment using Entity Framework Core
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
        var enrollment = new Enrollment
        {
            StudentId = studentId,
            StudentName = form["StudentName"],
            DateOfBirth = dateOfBirth,
            Gender = form["Gender"],
            Address = form["Address"],
            Email = form["Email"],
            PhoneNumber = form["PhoneNumber"],
            CourseId = courseId,
            EnrollmentDate = enrollmentDate,
            RegistrationDate = registrationDate
        };
        dbContext.Enrollments.Add(enrollment);
        await dbContext.SaveChangesAsync();

        // Return success response with created enrollment data
        context.Response.StatusCode = 201; // Created
        context.Response.Headers.Append("Location", $"/enrollments/{enrollment.Id}");
        await context.Response.WriteAsJsonAsync(enrollment);
    }
});

app.Run();

public class EnrollmentDbContext : DbContext
{
    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : base(options)
    {
    }

    public DbSet<Enrollment> Enrollments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrollment>().Property(e => e.Address).HasMaxLength(255);
    }
}

public record Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; init; }
    public string StudentName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; }
    public string Address { get; init; }
    public string Email { get; init; }
    public string PhoneNumber { get; init; }
    public int CourseId { get; init; }
    public DateTime EnrollmentDate { get; init; }
    public DateTime RegistrationDate { get; init; }
}
