using ITC.TrainingPortal.Application.DTOs;

namespace ITC.TrainingPortal.Application.Interfaces;

public interface IQuizService
{
    Task<IReadOnlyList<QuizDto>> GetAllAsync();
    Task<QuizDto?> GetAsync(int id);
    Task<int> CreateAsync(QuizDto dto);
    Task UpdateAsync(QuizDto dto);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<QuizQuestionDto>> GetQuestionsAsync(int quizId);
    Task<QuizQuestionDto?> GetQuestionAsync(int id);
    Task ImportQuestionsAsync(int quizId, IReadOnlyList<QuizQuestionDto> questions);
    Task AddQuestionAsync(QuizQuestionDto dto);
    Task UpdateQuestionAsync(QuizQuestionDto dto);
    Task DeleteQuestionAsync(int id);
    Task<IReadOnlyList<ExamAccessDto>> GetEmployeeExamsAsync(int employeeId);
    Task<ExamAccessDto?> GetAccessAsync(int employeeId, int quizId);
    Task<ExamAttemptDto> StartOrResumeAsync(int employeeId, int quizId);
    Task<QuizResultDto> SubmitAsync(int employeeId, ExamSubmissionDto submission);
    Task<IReadOnlyList<QuizResultDto>> GetEmployeeResultsAsync(int employeeId);
    Task<QuizResultDto?> GetResultAsync(int employeeId, int resultId);
    Task<bool> HasActiveAttemptAsync(int employeeId);
}
