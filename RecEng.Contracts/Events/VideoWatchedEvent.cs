namespace RecEng.Contracts.Events;

public record VideoWatchedEvent(Guid VideoId, Guid UserId, int WatchSeconds, DateTime OccurredAt);
