import json
import logging
from datetime import datetime, timezone
from typing import Optional
from uuid import UUID

from fastapi import APIRouter, Depends, HTTPException, Query, status
from pydantic import BaseModel
from sqlalchemy import select, update
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.auth import UserRole, require_role
from app.core.database import Alert, Device, SensorReading, Zone, get_db
from app.core.kafka_producer import get_producer
from app.core.redis_client import get_redis

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/sensors", tags=["sensors"])


# ── Schemas ────────────────────────────────────────────────────────────

class TelemetryPayload(BaseModel):
    device_id: str
    sensor_type: str
    value: float
    unit: Optional[str] = None
    timestamp: Optional[datetime] = None


class TelemetryResponse(BaseModel):
    reading_id: int
    device_id: str
    zone_id: Optional[UUID] = None
    sensor_type: str
    value: float
    recorded_at: datetime


class ReadingOut(BaseModel):
    id: int
    sensor_type: str
    value: float
    unit: Optional[str] = None
    recorded_at: datetime

    model_config = {"from_attributes": True}


# ── Alert thresholds ───────────────────────────────────────────────────

_ALERT_THRESHOLDS = {
    "temperature": (0.0, 40.0),
    "humidity": (20.0, 95.0),
    "ammonia_ppm": (0.0, 25.0),
    "water_ph": (6.0, 8.5),
}


# ── Endpoints ──────────────────────────────────────────────────────────

@router.post("/telemetry", response_model=TelemetryResponse, status_code=status.HTTP_201_CREATED)
async def ingest_telemetry(
    body: TelemetryPayload,
    db: AsyncSession = Depends(get_db),
    redis=Depends(get_redis),
    producer=Depends(get_producer),
):
    # Resolve device
    result = await db.execute(select(Device).where(Device.device_id == body.device_id, Device.is_active.is_(True)))
    device = result.scalar_one_or_none()
    if device is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Device {body.device_id} not found")

    # Update last seen
    await db.execute(update(Device).where(Device.id == device.id).values(last_seen_at=datetime.now(timezone.utc)))

    recorded_at = body.timestamp or datetime.now(timezone.utc)

    reading = SensorReading(
        device_id=device.id,
        zone_id=device.zone_id,
        sensor_type=body.sensor_type,
        value=body.value,
        unit=body.unit,
        recorded_at=recorded_at,
    )
    db.add(reading)
    await db.commit()
    await db.refresh(reading)

    # Publish to Kafka
    if producer is not None:
        event = {
            "reading_id": reading.id,
            "device_id": body.device_id,
            "zone_id": str(device.zone_id) if device.zone_id else None,
            "sensor_type": body.sensor_type,
            "value": body.value,
            "unit": body.unit,
            "recorded_at": recorded_at.isoformat(),
        }
        try:
            await producer.send_and_wait("nurain.sensor.telemetry", json.dumps(event).encode())
        except Exception as exc:
            logger.warning("Kafka publish failed: %s", exc)

    # Cache latest reading in Redis
    if redis is not None and device.zone_id is not None:
        cache_key = f"zone:{device.zone_id}:latest:{body.sensor_type}"
        try:
            await redis.set(cache_key, json.dumps({"value": body.value, "at": recorded_at.isoformat()}), ex=300)
        except Exception as exc:
            logger.warning("Redis cache failed: %s", exc)

    # Check alert thresholds
    await _check_thresholds(db, body.sensor_type, body.value, device.zone_id)

    return TelemetryResponse(
        reading_id=reading.id,
        device_id=body.device_id,
        zone_id=device.zone_id,
        sensor_type=body.sensor_type,
        value=body.value,
        recorded_at=recorded_at,
    )


@router.get("/readings/{zone_id}", response_model=list[ReadingOut])
async def get_readings(
    zone_id: UUID,
    sensor_type: Optional[str] = Query(None),
    limit: int = Query(50, ge=1, le=500),
    db: AsyncSession = Depends(get_db),
    _user=Depends(require_role(UserRole.VET)),
):
    stmt = (
        select(SensorReading)
        .where(SensorReading.zone_id == zone_id)
        .order_by(SensorReading.recorded_at.desc())
        .limit(limit)
    )
    if sensor_type:
        stmt = stmt.where(SensorReading.sensor_type == sensor_type)

    result = await db.execute(stmt)
    return result.scalars().all()


# ── Internals ──────────────────────────────────────────────────────────

async def _check_thresholds(db: AsyncSession, sensor_type: str, value: float, zone_id: Optional[UUID]) -> None:
    bounds = _ALERT_THRESHOLDS.get(sensor_type)
    if bounds is None or zone_id is None:
        return

    lo, hi = bounds
    if lo <= value <= hi:
        return

    severity = "HIGH" if (value < lo * 0.5 or value > hi * 1.5) else "MEDIUM"
    alert = Alert(
        zone_id=zone_id,
        severity=severity,
        alert_type=f"{sensor_type}_out_of_range",
        message=f"{sensor_type}={value:.2f} outside [{lo}, {hi}]",
    )
    db.add(alert)
    await db.commit()
    logger.info("Alert created: %s %s in zone %s", severity, sensor_type, zone_id)
