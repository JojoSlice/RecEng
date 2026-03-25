using api.Features.Users;

namespace api.Features.Videos;

public class VideoLike
{
    private VideoLike() { }

    public VideoLike(Guid userId, Guid videoId)
    {
        UserId = userId;
        VideoId = videoId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public Guid VideoId { get; private set; }
    public Video Video { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
}
