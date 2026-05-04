using Npgsql;
using StackExchange.Redis;

namespace recommendation_service;

public class Worker(
    ILogger<Worker> logger,
    [FromKeyedServices("postgres")] NpgsqlDataSource postgres,
    [FromKeyedServices("timescale")] NpgsqlDataSource timescale,
    IConnectionMultiplexer redis
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ComputeFeeds(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ComputeFeeds(CancellationToken ct)
    {
        var engagements = await GetEngagementScores(ct);
        var videos = await GetVideos(ct);
        var watchHistory = await GetWatchHistory(ct);

        logger.LogInformation(
            "Fetched {Videos} videos, {Engagements} engagement scores, {WatchEvents} watch events",
            videos.Count,
            engagements.Count,
            watchHistory.Count
        );

        var engagementByVideoId = engagements.ToDictionary(e => e.VideoId, e => e.EngagementScore);

        var videoScores = new List<VideoScore>();

        foreach (var video in videos)
        {
            engagementByVideoId.TryGetValue(video.Id, out var engagementScore);
            var hoursSinceUpload = (DateTimeOffset.UtcNow - video.CreatedAt).TotalHours;
            var trendingScore = engagementScore / Math.Pow(hoursSinceUpload + 2, 1.5);
            videoScores.Add(new VideoScore(video.Id, trendingScore));
        }
    }

    record VideoScore(Guid VideoId, double TrendingScore);

    private async Task<List<VideoEngagement>> GetEngagementScores(CancellationToken ct)
    {
        var results = new List<VideoEngagement>();
        await using var conn = await timescale.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT video_id, engagement_score FROM video_trending_score";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new VideoEngagement(reader.GetGuid(0), reader.GetInt64(1)));
        }
        return results;
    }

    record VideoEngagement(Guid VideoId, long EngagementScore);

    private async Task<List<Video>> GetVideos(CancellationToken ct)
    {
        var results = new List<Video>();
        await using var conn = await postgres.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT v."Id", v."CreatedAt",
                   COALESCE(array_agg(t."Name") FILTER (WHERE t."Name" IS NOT NULL), '{}') AS tags
            FROM "Videos" v
            LEFT JOIN "TagVideo" tv ON tv."VideosId" = v."Id"
            LEFT JOIN "Tags" t ON t."Id" = tv."TagsId"
            WHERE v."Status" = 1
            GROUP BY v."Id", v."CreatedAt"
            """;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(
                new Video(
                    reader.GetGuid(0),
                    reader.GetFieldValue<DateTimeOffset>(1),
                    reader.GetFieldValue<string[]>(2)
                )
            );
        }
        return results;
    }

    record Video(Guid Id, DateTimeOffset CreatedAt, string[] Tags);

    private async Task<List<WatchHistory>> GetWatchHistory(CancellationToken ct)
    {
        var results = new List<WatchHistory>();
        await using var conn = await timescale.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT user_id, video_id, SUM(watch_seconds) AS total_watch_seconds FROM video_interactions WHERE event_type = 'watched' GROUP BY user_id, video_id";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new WatchHistory(reader.GetGuid(0), reader.GetGuid(1), reader.GetInt64(2)));
        }
        return results;
    }

    record WatchHistory(Guid UserId, Guid VideoId, long TotalWatchSeconds);
}
