namespace api.Features.Videos;

public record VideoResponse(
    Guid Id,
    string Title,
    string Description,
    List<string> Tags,
    Guid UploadedBy,
    DateTimeOffset CreatedAt
)
{
    public static VideoResponse From(Video video) => new(
        video.Id,
        video.Title,
        video.Description,
        video.Tags.Select(t => t.Name).ToList(),
        video.UploadedBy,
        video.CreatedAt
    );
}
