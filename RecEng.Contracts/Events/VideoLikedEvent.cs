namespace RecEng.Contracts.Events;

public record VideoLikedEvent(Guid VideoId, Guid UserId, DateTime OccurredAt);
