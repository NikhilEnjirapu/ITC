namespace ITC.TrainingPortal.Application.Interfaces;

public interface IAssessmentTransaction
{
    Task<T> ExecuteAsync<T>(IReadOnlyList<string> resources, Func<Task<T>> operation);
}
