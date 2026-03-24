namespace api.Features.Users;

public class UserFollow
{
    private UserFollow() { }

    public UserFollow(Guid followerId, Guid followedId)
    {
        FollowerId = followerId;
        FollowedId = followedId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid FollowerId { get; private set; }
    public User Follower { get; private set; } = null!;
    public Guid FollowedId { get; private set; }
    public User Followed { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
}
