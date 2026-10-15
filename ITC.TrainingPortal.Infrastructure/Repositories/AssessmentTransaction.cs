using System.Data;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace ITC.TrainingPortal.Infrastructure.Repositories;

// Every assessment mutation uses transaction-owned locks, shared across app instances.
public sealed class AssessmentTransaction(ApplicationDbContext db) : IAssessmentTransaction
{
    public async Task<T> ExecuteAsync<T>(IReadOnlyList<string> resources, Func<Task<T>> operation)
    {
        // The in-memory provider is used only by isolated HTTP tests.
        if (!db.Database.IsRelational()) return await operation();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            foreach (var resource in resources.Distinct().Order(StringComparer.Ordinal))
            {
                var name = "TrainingPortal:assessment:" + resource;
                await db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource={name}, @LockMode='Exclusive',
    @LockOwner='Transaction', @LockTimeout=15000;
IF @result < 0 THROW 51000, 'Assessment is busy. Please try again.', 1;");
            }
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex) when (ex is SqlException { Number: 1205 or 51000 } ||
            ex is DbUpdateException { InnerException: SqlException { Number: 1205 or 51000 } })
        {
            throw new InvalidOperationException("The assessment changed in another request. Please reload and try again.", ex);
        }
        // Disposing an uncommitted transaction rolls back both business records and audit entries.
    }
}
