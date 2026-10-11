using System.Data;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Infrastructure.Repositories;

public sealed class TrainingProgramDeletion(ApplicationDbContext db) : ITrainingProgramDeletion
{
    public async Task DeleteAsync(int id)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        try
        {
            var program = await db.TrainingPrograms.FindAsync(id);
            if (program == null) return;
            // Break the program -> unlock video -> program cycle within the transaction.
            if (program.ExamUnlockVideoId.HasValue)
            {
                program.ExamUnlockCondition = ExamUnlockCondition.AllVideos;
                program.ExamUnlockVideoId = null;
                await db.SaveChangesAsync();
            }
            var videos = await db.TrainingVideos.Where(x => x.TrainingProgramId == id).ToListAsync();
            var videoIds = videos.Select(x => x.Id).ToArray();
            var exams = await db.Quizzes.Where(x => x.TrainingProgramId == id).ToListAsync();
            var examIds = exams.Select(x => x.Id).ToArray();
            db.VideoProgresses.RemoveRange(await db.VideoProgresses.Where(x => videoIds.Contains(x.TrainingVideoId)).ToListAsync());
            db.QuizResults.RemoveRange(await db.QuizResults.Where(x => examIds.Contains(x.QuizId)).ToListAsync());
            db.QuizQuestions.RemoveRange(await db.QuizQuestions.Where(x => examIds.Contains(x.QuizId)).ToListAsync());
            db.TrainingAssignments.RemoveRange(await db.TrainingAssignments.Where(x => x.TrainingProgramId == id).ToListAsync());
            db.Quizzes.RemoveRange(exams);
            db.TrainingVideos.RemoveRange(videos);
            db.TrainingPrograms.Remove(program);
            await db.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            if (transaction != null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            throw new InvalidOperationException("The training program could not be deleted because its records changed or are still referenced. Reload the page and try again.", ex);
        }
    }
}
