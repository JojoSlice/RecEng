using MassTransit;
using Npgsql;
using RecEng.Contracts.Events;

namespace analytics_service.Consumers;

public class VideoWatchedConsumer(NpgsqlDataSource db) : IConsumer<VideoWatchedEvent>
{
    public async Task Consume(ConsumeContext<VideoWatchedEvent> context)
    {
        var msg = context.Message;
        await using var conn = await db.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO video_interactions (event_type, video_id, user_id, watch_seconds, occurred_at)
            VALUES ('watched', @videoId, @userId, @watchSeconds, @occurredAt)
            """,
            conn
        );

        cmd.Parameters.AddWithValue("videoId", msg.VideoId);
        cmd.Parameters.AddWithValue("userId", msg.UserId);
        cmd.Parameters.AddWithValue("watchSeconds", msg.WatchSeconds);
        cmd.Parameters.AddWithValue("occurredAt", msg.OccurredAt);

        await cmd.ExecuteNonQueryAsync();
    }
}
