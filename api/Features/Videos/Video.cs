namespace api.Features.Videos;

public class Video
{
    private Video() { }

    public Video(string title, string description, string filePath, Guid uploadedBy)
    {
        Title = title;
        Description = description;
        FilePath = filePath;
        UploadedBy = uploadedBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public List<Tag> Tags { get; private set; } = [];
    public string FilePath { get; private set; } = null!;
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Update(string title, string description, List<Tag> tags)
    {
        Title = title;
        Description = description;
        Tags.Clear();
        Tags.AddRange(tags);
    }
}
