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
