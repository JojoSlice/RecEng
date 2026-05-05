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

        var watchTimeScores = watchHistory
            .GroupBy(w => w.UserId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var max = g.Max(w => w.TotalWatchSeconds);
                    return g.ToDictionary(w => w.VideoId, w => (double)w.TotalWatchSeconds / max);
                }
            );

        var uniqueTags = videos.SelectMany(v => v.Tags).Distinct().ToList();

        var tagVectors = videos
            .Select(v => new
            {
                VideoId = v.Id,
                Vector = uniqueTags.Select(tag => v.Tags.Contains(tag) ? 1.0 : 0.0).ToArray(),
            })
            .ToList();

        var videoTags = videos.ToDictionary(v => v.Id, v => v.Tags);
        var userWatchHistory = watchHistory.GroupBy(w => w.UserId);

        var userTagVectors = userWatchHistory.ToDictionary(
            g => g.Key,
            g =>
            {
                var seenTags = g.Where(w => videoTags.ContainsKey(w.VideoId))
                    .SelectMany(w => videoTags[w.VideoId])
                    .ToHashSet();
                return uniqueTags.Select(tag => seenTags.Contains(tag) ? 1.0 : 0.0).ToArray();
            }
        );

        var tagVectorByVideoId = tagVectors.ToDictionary(t => t.VideoId, t => t.Vector);

        var contentScores = userTagVectors.ToDictionary(
            kvp => kvp.Key,
            kvp =>
                tagVectorByVideoId.ToDictionary(
                    v => v.Key,
                    v => CosineSimilarity(kvp.Value, v.Value)
                )
        );

        var allVideoIds = videos.Select(v => v.Id).ToList();

        var userWatchVectors = watchTimeScores.ToDictionary(
            kvp => kvp.Key,
            kvp =>
                allVideoIds.Select(vid => kvp.Value.TryGetValue(vid, out var s) ? s : 0.0).ToArray()
        );

        var collaborativeScores = new Dictionary<Guid, Dictionary<Guid, double>>();

        foreach (var (userId, userVector) in userWatchVectors)
        {
            var similarUsers = userWatchVectors
                .Where(other => other.Key != userId)
                .Select(other =>
                    (UserId: other.Key, Similarity: CosineSimilarity(userVector, other.Value))
                )
                .Where(x => x.Similarity > 0)
                .OrderByDescending(x => x.Similarity)
                .Take(10)
                .ToList();

            var totalSimilarity = similarUsers.Sum(x => x.Similarity);
            var scores = new Dictionary<Guid, double>();

            if (totalSimilarity > 0)
            {
                foreach (var video in videos)
                {
                    scores[video.Id] =
                        similarUsers.Sum(su =>
                        {
                            watchTimeScores[su.UserId].TryGetValue(video.Id, out var s);
                            return su.Similarity * s;
                        }) / totalSimilarity;
                }
            }

            collaborativeScores[userId] = scores;
        }
    }

    record VideoScore(Guid VideoId, double TrendingScore);

    private static double CosineSimilarity(double[] a, double[] b)
    {
        var dot = a.Zip(b, (x, y) => x * y).Sum();
        var magA = Math.Sqrt(a.Sum(x => x * x));
        var magB = Math.Sqrt(b.Sum(x => x * x));
        return (magA == 0 || magB == 0) ? 0 : dot / (magA * magB);
    }

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
