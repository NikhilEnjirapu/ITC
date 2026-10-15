using ITC.TrainingPortal.Application.Interfaces;
namespace ITC.TrainingPortal.Tests;
internal sealed class TestAssessmentTransaction : IAssessmentTransaction
{
    private readonly SemaphoreSlim gate = new(1);
    public async Task<T> ExecuteAsync<T>(IReadOnlyList<string> resources, Func<Task<T>> operation)
    {
        await gate.WaitAsync();
        try { return await operation(); }
        finally { gate.Release(); }
    }
}
