namespace ITC.TrainingPortal.Domain.Exceptions;

public sealed class PlaybackTimingException(int acceptedPositionSeconds, double requestedPositionSeconds, double elapsedSeconds, double creditSeconds)
    : InvalidOperationException("Playback progress was rejected because it advanced too quickly.")
{
    public int AcceptedPositionSeconds { get; } = acceptedPositionSeconds;
    public double RequestedPositionSeconds { get; } = requestedPositionSeconds;
    public double ElapsedSeconds { get; } = elapsedSeconds;
    public double CreditSeconds { get; } = creditSeconds;
}
