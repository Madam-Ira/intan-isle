import uuid
from datetime import datetime, timezone
from typing import AsyncGenerator

from sqlalchemy import (
    Boolean,
    Column,
    DateTime,
    Float,
    ForeignKey,
    Integer,
    String,
    Text,
    BigInteger,
    UniqueConstraint,
    Index,
)
from sqlalchemy.dialects.postgresql import UUID
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker, create_async_engine
from sqlalchemy.orm import DeclarativeBase

from app.core.config import settings

# ── Engine & session factory ────────────────────────────────────────────

engine = create_async_engine(settings.DATABASE_URL, echo=False, pool_size=10, max_overflow=20)
async_session_factory = async_sessionmaker(engine, class_=AsyncSession, expire_on_commit=False)


# ── Declarative base ───────────────────────────────────────────────────

class Base(DeclarativeBase):
    pass


def _utcnow() -> datetime:
    return datetime.now(timezone.utc)


# ── ORM Models ─────────────────────────────────────────────────────────

class User(Base):
    __tablename__ = "users"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    username = Column(String(100), unique=True, nullable=False)
    email = Column(String(255), unique=True, nullable=False)
    hashed_password = Column(String(255), nullable=False)
    role = Column(String(20), nullable=False, default="VIEWER")
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime(timezone=True), default=_utcnow)


class Zone(Base):
    __tablename__ = "zones"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    name = Column(String(200), nullable=False)
    zone_type = Column(Integer, nullable=False)
    latitude = Column(Float)
    longitude = Column(Float)
    pollution_level = Column(Float, default=0.0)
    healing_actions_applied = Column(Integer, default=0)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime(timezone=True), default=_utcnow)


class Device(Base):
    __tablename__ = "devices"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    device_id = Column(String(100), unique=True, nullable=False)
    device_type = Column(String(50), nullable=False)
    zone_id = Column(UUID(as_uuid=True), ForeignKey("zones.id"))
    certificate_fingerprint = Column(String(255))
    is_active = Column(Boolean, default=True)
    last_seen_at = Column(DateTime(timezone=True))
    created_at = Column(DateTime(timezone=True), default=_utcnow)


class SensorReading(Base):
    __tablename__ = "sensor_readings"

    id = Column(BigInteger, primary_key=True, autoincrement=True)
    device_id = Column(UUID(as_uuid=True), ForeignKey("devices.id"))
    zone_id = Column(UUID(as_uuid=True), ForeignKey("zones.id"))
    sensor_type = Column(String(50), nullable=False)
    value = Column(Float, nullable=False)
    unit = Column(String(20))
    recorded_at = Column(DateTime(timezone=True), default=_utcnow)

    __table_args__ = (
        Index("idx_sensor_readings_zone", "zone_id", recorded_at.desc()),
        Index("idx_sensor_readings_device", "device_id", recorded_at.desc()),
    )


class Alert(Base):
    __tablename__ = "alerts"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    zone_id = Column(UUID(as_uuid=True), ForeignKey("zones.id"))
    severity = Column(String(20), nullable=False)
    alert_type = Column(String(100), nullable=False)
    message = Column(Text)
    is_resolved = Column(Boolean, default=False)
    created_at = Column(DateTime(timezone=True), default=_utcnow)
    resolved_at = Column(DateTime(timezone=True))

    __table_args__ = (
        Index("idx_alerts_open", "is_resolved", "severity"),
    )


class Rabbit(Base):
    __tablename__ = "rabbits"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    name = Column(String(100), nullable=False)
    breed = Column(String(50), nullable=False)
    zone_id = Column(UUID(as_uuid=True), ForeignKey("zones.id"))
    health_state = Column(String(30), nullable=False, default="HEALTHY")
    is_distressed = Column(Boolean, default=False)
    last_fed_at = Column(DateTime(timezone=True))
    last_watered_at = Column(DateTime(timezone=True))
    created_at = Column(DateTime(timezone=True), default=_utcnow)


class OverrideLog(Base):
    __tablename__ = "override_logs"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    action = Column(String(100), nullable=False)
    target_type = Column(String(50), nullable=False)
    target_id = Column(UUID(as_uuid=True), nullable=False)
    requested_by = Column(UUID(as_uuid=True), ForeignKey("users.id"))
    approved_by = Column(UUID(as_uuid=True), ForeignKey("users.id"))
    reason = Column(Text, nullable=False)
    status = Column(String(20), nullable=False, default="PENDING")
    created_at = Column(DateTime(timezone=True), default=_utcnow)
    approved_at = Column(DateTime(timezone=True))


class LearnerProfile(Base):
    __tablename__ = "learner_profiles"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    player_id = Column(String(100), unique=True, nullable=False)
    curiosity_index = Column(Float, default=0.0)
    persistence_index = Column(Float, default=0.0)
    empathy_index = Column(Float, default=0.0)
    spatial_awareness = Column(Float, default=0.0)
    ecological_literacy = Column(Float, default=0.0)
    cultural_sensitivity = Column(Float, default=0.0)
    spiritual_resonance = Column(Float, default=0.0)
    dominant_style = Column(Integer, default=0)
    challenge_pref = Column(Integer, default=1)
    engagement_level = Column(Integer, default=2)
    frustration_tolerance = Column(Float, default=0.5)
    session_count = Column(Integer, default=0)
    total_play_time_min = Column(Float, default=0.0)
    updated_at = Column(DateTime(timezone=True), default=_utcnow)


class IEPGoal(Base):
    __tablename__ = "iep_goals"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    player_id = Column(String(100), nullable=False)
    goal_id = Column(String(100), nullable=False)
    description = Column(Text)
    metric = Column(Integer, nullable=False)
    target_value = Column(Float, nullable=False)
    current_value = Column(Float, default=0.0)
    status = Column(Integer, default=0)
    created_at = Column(DateTime(timezone=True), default=_utcnow)
    achieved_at = Column(DateTime(timezone=True))

    __table_args__ = (
        UniqueConstraint("player_id", "goal_id", name="uq_player_goal"),
    )


# ── Lifecycle ──────────────────────────────────────────────────────────

async def init_db() -> None:
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)


async def close_db() -> None:
    await engine.dispose()


# ── FastAPI dependency ─────────────────────────────────────────────────

async def get_db() -> AsyncGenerator[AsyncSession, None]:
    async with async_session_factory() as session:
        yield session
