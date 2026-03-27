using MassTransit;
using Npgsql;
using RecEng.Contracts.Events;

namespace analytics_service.Consumers;

public class VideoUnlikedConsumer(NpgsqlDataSource db) : IConsumer<VideoUnlikedEvent>
{
    public async Task Consume(ConsumeContext<VideoUnlikedEvent> context)
    {
        var msg = context.Message;
        await using var conn = await db.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO video_interactions (event_type, video_id, user_id, occurred_at)
            VALUES ('unliked', @videoId, @userId, @occurredAt)
            """,
            conn
        );

        cmd.Parameters.AddWithValue("videoId", msg.VideoId);
        cmd.Parameters.AddWithValue("userId", msg.UserId);
        cmd.Parameters.AddWithValue("occurredAt", msg.OccurredAt);

        await cmd.ExecuteNonQueryAsync();
    }
}
