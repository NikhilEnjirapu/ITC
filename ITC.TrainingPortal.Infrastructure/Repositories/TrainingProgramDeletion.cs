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
            // Keep the complete graph: reports and historical results reference it.
            program.IsArchived = true;
            program.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            if (transaction != null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            throw new InvalidOperationException("The training program could not be archived because its records changed or are still referenced. Reload the page and try again.", ex);
        }
    }
}
