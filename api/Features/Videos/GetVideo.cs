using api.Data;

namespace api.Features.Videos;

public static class GetVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{id}", Handle);
    }

    public static async Task<IResult> Handle(Guid id, AppDbContext db, ILogger<AppDbContext> logger)
    {
        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == id);
        
        if(video is null)
        {
            logger.LogWarning("GetVideo failed, Video with Id: {Id} was not found", id);
            return Results.NotFound(new { message = "Video not found"});
        }

        logger.LogInformation("GetVideo Id: {Id} returned", video.Id);
        return Results.Ok(video);
    }
}

