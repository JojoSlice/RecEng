namespace api.Features.Videos;

public enum VideoStatus { Processing, Ready, Failed }

public class Video
{
    private Video() { }

    public Video(string title, string description, Guid uploadedBy)
    {
        Title = title;
        Description = description;
        FilePath = string.Empty;
        UploadedBy = uploadedBy;
        CreatedAt = DateTimeOffset.UtcNow;
        Status = VideoStatus.Processing;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public List<Tag> Tags { get; private set; } = [];
    public string FilePath { get; private set; } = null!;
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public VideoStatus Status { get; private set; }

    public void SetReady(string filePath)
    {
        FilePath = filePath;
        Status = VideoStatus.Ready;
    }

    public void SetFailed()
    {
        Status = VideoStatus.Failed;
    }

    public void Update(string title, string description, List<Tag> tags)
    {
        Title = title;
        Description = description;
        Tags.Clear();
        Tags.AddRange(tags);
    }
}
