namespace RecEng.Contracts.Events;

public record VideoUnlikedEvent(Guid VideoId, Guid UserId, DateTime OccurredAt);
