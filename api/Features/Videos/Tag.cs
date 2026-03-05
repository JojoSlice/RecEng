namespace api.Features.Videos;

public class Tag
{
    private Tag() { }

    public Tag(string name)
    {
        Name = name.ToLowerInvariant();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public List<Video> Videos { get; private set; } = [];
}
