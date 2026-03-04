namespace api.Features.Videos;

public class Video
{
    private Video() { }

    public Video(string title, string filePath, Guid uploadedBy)
    {
        Title = title;
        FilePath = filePath;
        UploadedBy = uploadedBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string FilePath { get; private set; } = null!;
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
