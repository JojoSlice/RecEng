CREATE EXTENSION IF NOT EXISTS timescaledb;

CREATE TABLE IF NOT EXISTS video_interactions (
      id            BIGSERIAL,
      event_type    TEXT        NOT NULL,
      video_id      UUID        NOT NULL,
      user_id       UUID        NOT NULL,
      watch_seconds INT         NULL,
      occurred_at   TIMESTAMPTZ NOT NULL
  );

  SELECT create_hypertable('video_interactions', by_range('occurred_at'));

  CREATE INDEX ON video_interactions (video_id, occurred_at DESC);
  CREATE INDEX ON video_interactions (user_id, occurred_at DESC);

CREATE MATERIALIZED VIEW video_engagement_1h
  WITH (timescaledb.continuous = true, timescaledb.materialized_only = false)
  AS
  SELECT
      time_bucket('1 hour', occurred_at) AS bucket,
      video_id,
      COUNT(*) FILTER (WHERE event_type = 'watched')   AS watch_count,
      SUM(watch_seconds) FILTER (WHERE event_type = 'watched') AS total_watch_seconds,
      COUNT(*) FILTER (WHERE event_type = 'liked')     AS like_count,
      COUNT(*) FILTER (WHERE event_type = 'unliked')   AS unlike_count
  FROM video_interactions
  GROUP BY bucket, video_id;

SELECT add_continuous_aggregate_policy('video_engagement_1h',
      start_offset => INTERVAL '3 hours',
      end_offset   => INTERVAL '1 hour',
      schedule_interval => INTERVAL '30 minutes'
  );

  CREATE VIEW video_trending_score AS
  SELECT
      video_id,
      SUM(like_count * 3 + watch_count) AS engagement_score
  FROM video_engagement_1h
  WHERE bucket > NOW() - INTERVAL '48 hours'
  GROUP BY video_id;

CREATE INDEX ON video_engagement_1h (video_id, bucket DESC);
