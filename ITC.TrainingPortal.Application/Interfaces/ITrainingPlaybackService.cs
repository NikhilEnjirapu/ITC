using ITC.TrainingPortal.Application.DTOs;

namespace ITC.TrainingPortal.Application.Interfaces;

public interface ITrainingPlaybackService
{
    Task<TrainingContentDto?> GetAssignedContentAsync(int employeeId, int trainingProgramId);
    Task<VideoPlaybackDto?> OpenVideoAsync(int employeeId, int videoId);
    Task<VideoHeartbeatResult> RecordHeartbeatAsync(int employeeId, VideoHeartbeatRequest request);
}
