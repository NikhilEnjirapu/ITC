using ITC.TrainingPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public string? AuditUserId { get; set; }
    public string? AuditEmployeeId { get; set; }
    public string? AuditIpAddress { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var changes = ChangeTracker.Entries<BaseEntity>()
            .Where(x => x.Entity is not AuditLog && x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => new { Entry = entry, State = entry.State,
                Before = entry.Properties.Where(x => x.Metadata.Name != "RowVersion")
                    .ToDictionary(x => x.Metadata.Name, x => entry.State == EntityState.Added ? null : x.OriginalValue) }).ToList();
        if (changes.Count == 0) return await base.SaveChangesAsync(cancellationToken);
        await using var transaction = Database.IsRelational() && Database.CurrentTransaction == null
            ? await Database.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            var count = await base.SaveChangesAsync(cancellationToken);
            foreach (var change in changes)
            {
                var details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    change.Entry.Entity.Id,
                    Values = change.Entry.Properties.Where(x => x.Metadata.Name != "RowVersion")
                        .ToDictionary(x => x.Metadata.Name, x => new { Before = change.Before.GetValueOrDefault(x.Metadata.Name),
                            After = change.State == EntityState.Deleted ? null : x.CurrentValue })
                });
                AuditLogs.Add(new AuditLog { Action = $"{change.Entry.Entity.GetType().Name}.{change.State}",
                    UserId = AuditUserId, EmployeeId = AuditEmployeeId, IpAddress = AuditIpAddress, Details = details });
            }
            await base.SaveChangesAsync(cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            return count;
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<TrainingVideo> TrainingVideos => Set<TrainingVideo>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<TrainingAssignment> TrainingAssignments => Set<TrainingAssignment>();
    public DbSet<VideoProgress> VideoProgresses => Set<VideoProgress>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<EligibilityRule> EligibilityRules => Set<EligibilityRule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.Property(d => d.Name).HasMaxLength(100).IsRequired();
            entity.Property(d => d.Description).HasMaxLength(500);
            entity.Property(d => d.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.WorkerType).HasMaxLength(30).IsRequired();
            entity.Property(e => e.EmployeeId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<TrainingProgram>(entity =>
        {
            entity.Property(x => x.ExamUnlockCondition).HasDefaultValue(ExamUnlockCondition.AllVideos);
            entity.HasOne(x => x.ExamUnlockVideo).WithMany()
                .HasForeignKey(x => x.ExamUnlockVideoId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_TrainingPrograms_ExamUnlock",
                "([ExamUnlockCondition] = 0 AND [ExamUnlockVideoId] IS NULL) OR ([ExamUnlockCondition] = 1 AND [ExamUnlockVideoId] IS NOT NULL)"));
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<TrainingVideo>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.VideoUrl).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.TrainingProgramId, x.SequenceOrder }).IsUnique();
            entity.HasOne(v => v.TrainingProgram).WithMany(p => p.Videos)
                .HasForeignKey(v => v.TrainingProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainingAssignment>(entity =>
        {
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.EmployeeId, x.TrainingProgramId }).IsUnique();
            entity.HasOne(x => x.Employee).WithMany(x => x.Assignments)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TrainingProgram).WithMany(x => x.Assignments)
                .HasForeignKey(x => x.TrainingProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasIndex(x => x.TrainingProgramId).IsUnique();
            entity.HasOne(q => q.TrainingProgram).WithOne(p => p.Quiz)
                .HasForeignKey<Quiz>(q => q.TrainingProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.Property(x => x.AnswerKeywords).HasMaxLength(2000);
            entity.HasIndex(x => new { x.QuizId, x.SequenceOrder }).IsUnique();
            entity.HasOne(q => q.Quiz).WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.EmployeeId).IsUnique().HasFilter("[IsCompleted] = 0");
            entity.HasIndex(x => new { x.EmployeeId, x.QuizId, x.IsCompleted });
            entity.HasOne(x => x.Employee).WithMany(x => x.QuizResults)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Quiz).WithMany(x => x.Results)
                .HasForeignKey(x => x.QuizId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VideoProgress>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<VideoProgress>()
            .HasIndex(vp => new { vp.EmployeeId, vp.TrainingVideoId })
            .IsUnique();
    }
}
