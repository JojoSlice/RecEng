namespace api.Features.Videos;

public record UploaderResponse(Guid Id, string Username);

public record VideoResponse(
    Guid Id,
    string Title,
    string Description,
    List<string> Tags,
    UploaderResponse Uploader,
    DateTimeOffset CreatedAt
)
{
    public static VideoResponse From(Video video, string uploaderUsername) => new(
        video.Id,
        video.Title,
        video.Description,
        video.Tags.Select(t => t.Name).ToList(),
        new UploaderResponse(video.UploadedBy, uploaderUsername),
        video.CreatedAt
    );
}
