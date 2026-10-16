using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Authentication;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Web.Services;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, string contentRoot, IConfiguration configuration)
    {
        var source = Path.GetFullPath(Path.Combine(contentRoot, ".."));
        var modules = new[]
        {
            (Title: "Module 1 - Code of Conduct", Video: "Module-1 5.mp4", Exam: "Module-1_Code-of-Conduct (1).csv", Seconds: 171),
            (Title: "Module 2 - Good Corporate Citizenship", Video: "Module-2 3.mp4", Exam: "Module-2_Good-Corporate-Citizenship (1).csv", Seconds: 315),
            (Title: "Module 3 - Personal Conduct", Video: "Module-3.mp4", Exam: "Module-3_Personal-Conduct (1).csv", Seconds: 496)
        };
        // Validate all source files before writing any database records.
        var questions = modules.Select(module =>
        {
            var videoPath = Path.Combine(source, "Videos", module.Video);
            using var video = File.OpenRead(videoPath);
            var header = new byte[12];
            if (video.Read(header) != 12 || !header.AsSpan(4, 4).SequenceEqual("ftyp"u8))
                throw new InvalidOperationException($"Download the actual MP4 with git lfs pull: {module.Video}");
            using var reader = File.OpenText(Path.Combine(source, "Exam", module.Exam));
            // The parser validates a positive quiz ID; entities receive their real FK below.
            return QuestionCsvReader.Read(reader, 1);
        }).ToArray();
        var repository = Path.GetFullPath(configuration["VideoRepository:Path"] ?? Path.Combine(contentRoot, "App_Data", "Videos"));
        Directory.CreateDirectory(repository);
        foreach (var module in modules)
        {
            var destination = Path.Combine(repository, module.Video);
            if (!File.Exists(destination)) File.Copy(Path.Combine(source, "Videos", module.Video), destination);
        }

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        var department = await db.Departments.FirstOrDefaultAsync(x => x.Name == "Development Testing");
        if (department == null)
        {
            department = new Department { Name = "Development Testing", Description = "Local development test accounts" };
            db.Departments.Add(department);
        }
        var employees = new List<Employee>();
        for (var number = 0; number <= 5; number++)
        {
            var email = number == 0 ? DevelopmentAuthentication.AdminEmail : DevelopmentAuthentication.TestEmployeeEmail(number);
            var employee = await db.Employees.SingleOrDefaultAsync(x => x.Email == email);
            if (employee == null)
            {
                employee = new Employee
                {
                    EmployeeId = (900001 + number).ToString(), FirstName = "Local",
                    LastName = number == 0 ? "Administrator" : $"Employee {number}",
                    Email = email, UserId = number == 0 ? DevelopmentAuthentication.AdminUserId : DevelopmentAuthentication.TestEmployeeUserId(number),
                    WorkerType = "Employee", Department = department
                };
                db.Employees.Add(employee);
            }
            if (number > 0) employees.Add(employee);
        }
        await db.SaveChangesAsync();
        for (var index = 0; index < modules.Length; index++)
        {
            var module = modules[index];
            var program = await db.TrainingPrograms.SingleOrDefaultAsync(x => x.Title == module.Title);
            if (program == null)
            {
                program = new TrainingProgram { Title = module.Title, Description = "Development testing: watch the module, then complete its exam." };
                program.Videos.Add(new TrainingVideo { Title = module.Title, VideoUrl = module.Video, SequenceOrder = 1, DurationSeconds = module.Seconds });
                program.Quiz = new Quiz
                {
                    Title = module.Title + " - Exam", PassingScorePercentage = 80,
                    Questions = questions[index].Select(q => new QuizQuestion
                    {
                        QuestionText = q.QuestionText, IsTypedAnswer = q.IsTypedAnswer,
                        OptionA = q.OptionA ?? "", OptionB = q.OptionB ?? "", OptionC = q.OptionC ?? "", OptionD = q.OptionD ?? "",
                        CorrectOption = q.CorrectOption ?? "", AnswerKeywords = q.AnswerKeywords ?? "", SequenceOrder = q.SequenceOrder
                    }).ToList()
                };
                db.TrainingPrograms.Add(program);
                await db.SaveChangesAsync();
            }
            foreach (var employee in employees)
                if (!await db.TrainingAssignments.AnyAsync(x => x.EmployeeId == employee.Id && x.TrainingProgramId == program.Id))
                    db.TrainingAssignments.Add(new TrainingAssignment { EmployeeId = employee.Id, TrainingProgramId = program.Id });
            await db.SaveChangesAsync();
        }
        if (transaction != null) await transaction.CommitAsync();
        Console.WriteLine("Development data ready: 3 programs with videos and exams, 5 employees, and their assignments. Existing records and progress preserved.");
    }
}
