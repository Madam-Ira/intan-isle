-- NurAIN MVP Schema
-- Matches C# data contracts: IntanIsleZoneData, RabbitCareManager,
-- LearnerProfileVector, SessionBehaviourVector, IEPGoal

CREATE TABLE IF NOT EXISTS users (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username        VARCHAR(100) UNIQUE NOT NULL,
    email           VARCHAR(255) UNIQUE NOT NULL,
    hashed_password VARCHAR(255) NOT NULL,
    role            VARCHAR(20)  NOT NULL DEFAULT 'VIEWER',
    is_active       BOOLEAN      DEFAULT TRUE,
    created_at      TIMESTAMPTZ  DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS zones (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name                    VARCHAR(200) NOT NULL,
    zone_type               INTEGER NOT NULL,
    latitude                DOUBLE PRECISION,
    longitude               DOUBLE PRECISION,
    pollution_level         REAL DEFAULT 0.0,
    healing_actions_applied INTEGER DEFAULT 0,
    is_active               BOOLEAN DEFAULT TRUE,
    created_at              TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS devices (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id               VARCHAR(100) UNIQUE NOT NULL,
    device_type             VARCHAR(50) NOT NULL,
    zone_id                 UUID REFERENCES zones(id),
    certificate_fingerprint VARCHAR(255),
    is_active               BOOLEAN DEFAULT TRUE,
    last_seen_at            TIMESTAMPTZ,
    created_at              TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sensor_readings (
    id          BIGSERIAL PRIMARY KEY,
    device_id   UUID REFERENCES devices(id),
    zone_id     UUID REFERENCES zones(id),
    sensor_type VARCHAR(50) NOT NULL,
    value       REAL NOT NULL,
    unit        VARCHAR(20),
    recorded_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS alerts (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    zone_id     UUID REFERENCES zones(id),
    severity    VARCHAR(20)  NOT NULL,
    alert_type  VARCHAR(100) NOT NULL,
    message     TEXT,
    is_resolved BOOLEAN DEFAULT FALSE,
    created_at  TIMESTAMPTZ DEFAULT NOW(),
    resolved_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS rabbits (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(100) NOT NULL,
    breed           VARCHAR(50)  NOT NULL,
    zone_id         UUID REFERENCES zones(id),
    health_state    VARCHAR(30)  NOT NULL DEFAULT 'HEALTHY',
    is_distressed   BOOLEAN DEFAULT FALSE,
    last_fed_at     TIMESTAMPTZ,
    last_watered_at TIMESTAMPTZ,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS override_logs (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    action        VARCHAR(100) NOT NULL,
    target_type   VARCHAR(50)  NOT NULL,
    target_id     UUID         NOT NULL,
    requested_by  UUID REFERENCES users(id),
    approved_by   UUID REFERENCES users(id),
    reason        TEXT         NOT NULL,
    status        VARCHAR(20)  NOT NULL DEFAULT 'PENDING',
    created_at    TIMESTAMPTZ  DEFAULT NOW(),
    approved_at   TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS learner_profiles (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id             VARCHAR(100) UNIQUE NOT NULL,
    curiosity_index       REAL DEFAULT 0.0,
    persistence_index     REAL DEFAULT 0.0,
    empathy_index         REAL DEFAULT 0.0,
    spatial_awareness     REAL DEFAULT 0.0,
    ecological_literacy   REAL DEFAULT 0.0,
    cultural_sensitivity  REAL DEFAULT 0.0,
    spiritual_resonance   REAL DEFAULT 0.0,
    dominant_style        INTEGER DEFAULT 0,
    challenge_pref        INTEGER DEFAULT 1,
    engagement_level      INTEGER DEFAULT 2,
    frustration_tolerance REAL DEFAULT 0.5,
    session_count         INTEGER DEFAULT 0,
    total_play_time_min   REAL DEFAULT 0.0,
    updated_at            TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS iep_goals (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id     VARCHAR(100) NOT NULL,
    goal_id       VARCHAR(100) NOT NULL,
    description   TEXT,
    metric        INTEGER NOT NULL,
    target_value  REAL    NOT NULL,
    current_value REAL    DEFAULT 0.0,
    status        INTEGER DEFAULT 0,
    created_at    TIMESTAMPTZ DEFAULT NOW(),
    achieved_at   TIMESTAMPTZ,
    UNIQUE(player_id, goal_id)
);

CREATE INDEX IF NOT EXISTS idx_sensor_readings_zone   ON sensor_readings(zone_id, recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_sensor_readings_device ON sensor_readings(device_id, recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_alerts_open            ON alerts(is_resolved, severity);
CREATE INDEX IF NOT EXISTS idx_learner_player         ON learner_profiles(player_id);
