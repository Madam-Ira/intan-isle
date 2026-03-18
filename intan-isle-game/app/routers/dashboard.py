import json
from typing import Optional
from uuid import UUID

from fastapi import APIRouter, Depends, Query
from pydantic import BaseModel
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.auth import UserRole, require_role
from app.core.database import Alert, Rabbit, Zone, get_db
from app.core.redis_client import get_redis

router = APIRouter(prefix="/dashboard", tags=["dashboard"])


# ── Schemas ────────────────────────────────────────────────────────────

class ZoneHealth(BaseModel):
    zone_id: UUID
    zone_name: str
    zone_type: int
    pollution_level: float
    healing_actions: int
    open_alerts: int
    cached_sensors: dict


class ZoneOut(BaseModel):
    id: UUID
    name: str
    zone_type: int
    latitude: Optional[float] = None
    longitude: Optional[float] = None
    pollution_level: float
    is_active: bool

    model_config = {"from_attributes": True}


class AlertOut(BaseModel):
    id: UUID
    zone_id: Optional[UUID] = None
    severity: str
    alert_type: str
    message: Optional[str] = None
    is_resolved: bool
    created_at: str

    model_config = {"from_attributes": True}


class WelfareSummary(BaseModel):
    total_rabbits: int
    healthy: int
    unwell: int
    distressed: int
    distress_rate: float


# ── Endpoints ──────────────────────────────────────────────────────────

@router.get("/health", response_model=list[ZoneHealth])
async def farm_health(
    db: AsyncSession = Depends(get_db),
    redis=Depends(get_redis),
    _user=Depends(require_role(UserRole.VET)),
):
    zones_result = await db.execute(select(Zone).where(Zone.is_active.is_(True)))
    zones = zones_result.scalars().all()

    out = []
    for z in zones:
        # Count open alerts
        alert_count = await db.execute(
            select(func.count()).select_from(Alert).where(Alert.zone_id == z.id, Alert.is_resolved.is_(False))
        )
        open_alerts = alert_count.scalar() or 0

        # Get cached sensor data from Redis
        cached = {}
        if redis is not None:
            for sensor in ("temperature", "humidity", "ammonia_ppm", "water_ph"):
                raw = await redis.get(f"zone:{z.id}:latest:{sensor}")
                if raw:
                    cached[sensor] = json.loads(raw)

        out.append(
            ZoneHealth(
                zone_id=z.id,
                zone_name=z.name,
                zone_type=z.zone_type,
                pollution_level=z.pollution_level,
                healing_actions=z.healing_actions_applied,
                open_alerts=open_alerts,
                cached_sensors=cached,
            )
        )

    return out


@router.get("/zones", response_model=list[ZoneOut])
async def list_zones(
    db: AsyncSession = Depends(get_db),
    _user=Depends(require_role(UserRole.VET)),
):
    result = await db.execute(select(Zone).where(Zone.is_active.is_(True)).order_by(Zone.name))
    return result.scalars().all()


@router.get("/alerts", response_model=list[AlertOut])
async def list_alerts(
    severity: Optional[str] = Query(None, description="Filter: LOW, MEDIUM, HIGH, CRITICAL"),
    resolved: bool = Query(False),
    limit: int = Query(100, ge=1, le=500),
    db: AsyncSession = Depends(get_db),
    _user=Depends(require_role(UserRole.VET)),
):
    stmt = select(Alert).where(Alert.is_resolved == resolved).order_by(Alert.created_at.desc()).limit(limit)
    if severity:
        stmt = stmt.where(Alert.severity == severity.upper())

    result = await db.execute(stmt)
    rows = result.scalars().all()

    return [
        AlertOut(
            id=a.id,
            zone_id=a.zone_id,
            severity=a.severity,
            alert_type=a.alert_type,
            message=a.message,
            is_resolved=a.is_resolved,
            created_at=a.created_at.isoformat() if a.created_at else "",
        )
        for a in rows
    ]


@router.get("/welfare", response_model=WelfareSummary)
async def rabbit_welfare(
    db: AsyncSession = Depends(get_db),
    _user=Depends(require_role(UserRole.VET)),
):
    result = await db.execute(select(Rabbit))
    rabbits = result.scalars().all()

    total = len(rabbits)
    distressed = sum(1 for r in rabbits if r.is_distressed)
    unwell = sum(1 for r in rabbits if r.health_state != "HEALTHY" and not r.is_distressed)
    healthy = total - distressed - unwell

    return WelfareSummary(
        total_rabbits=total,
        healthy=healthy,
        unwell=unwell,
        distressed=distressed,
        distress_rate=distressed / max(total, 1),
    )
