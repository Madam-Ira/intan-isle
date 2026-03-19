from datetime import datetime, timezone
from typing import Optional
from uuid import UUID

from fastapi import APIRouter, Depends, HTTPException, Query, status
from pydantic import BaseModel
from sqlalchemy import select, update
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.auth import UserRole, require_role
from app.core.database import OverrideLog, User, get_db

router = APIRouter(prefix="/overrides", tags=["overrides"])


# ── Schemas ────────────────────────────────────────────────────────────

class OverrideRequest(BaseModel):
    action: str
    target_type: str
    target_id: UUID
    reason: str


class ApproveRequest(BaseModel):
    approved: bool


class OverrideOut(BaseModel):
    id: UUID
    action: str
    target_type: str
    target_id: UUID
    requested_by: Optional[UUID] = None
    approved_by: Optional[UUID] = None
    reason: str
    status: str
    created_at: str
    approved_at: Optional[str] = None

    model_config = {"from_attributes": True}


# ── Endpoints ──────────────────────────────────────────────────────────

@router.post("/", response_model=OverrideOut, status_code=status.HTTP_201_CREATED)
async def create_override(
    body: OverrideRequest,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_role(UserRole.VET)),
):
    """Create an override request. Requires at least VET role."""
    log_entry = OverrideLog(
        action=body.action,
        target_type=body.target_type,
        target_id=body.target_id,
        requested_by=current_user.id,
        reason=body.reason,
        status="PENDING",
    )
    db.add(log_entry)
    await db.commit()
    await db.refresh(log_entry)

    return _to_out(log_entry)


@router.put("/{override_id}/approve", response_model=OverrideOut)
async def approve_override(
    override_id: UUID,
    body: ApproveRequest,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_role(UserRole.FARM_MANAGER)),
):
    """Approve or reject an override. Requires FARM_MANAGER+. Dual-auth: approver != requester."""
    result = await db.execute(select(OverrideLog).where(OverrideLog.id == override_id))
    entry = result.scalar_one_or_none()

    if entry is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Override not found")

    if entry.status != "PENDING":
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail=f"Override already {entry.status}")

    # Dual auth: approver must differ from requester
    if entry.requested_by == current_user.id:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Dual-auth required: approver must differ from requester",
        )

    new_status = "APPROVED" if body.approved else "REJECTED"
    now = datetime.now(timezone.utc)

    await db.execute(
        update(OverrideLog)
        .where(OverrideLog.id == override_id)
        .values(status=new_status, approved_by=current_user.id, approved_at=now)
    )
    await db.commit()

    result = await db.execute(select(OverrideLog).where(OverrideLog.id == override_id))
    entry = result.scalar_one_or_none()

    if entry is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Override lost during approval")

    return _to_out(entry)


@router.get("/", response_model=list[OverrideOut])
async def list_overrides(
    status_filter: Optional[str] = Query(None, alias="status", description="PENDING, APPROVED, REJECTED"),
    limit: int = Query(50, ge=1, le=200),
    db: AsyncSession = Depends(get_db),
    _user=Depends(require_role(UserRole.VET)),
):
    stmt = select(OverrideLog).order_by(OverrideLog.created_at.desc()).limit(limit)
    if status_filter:
        stmt = stmt.where(OverrideLog.status == status_filter.upper())

    result = await db.execute(stmt)
    return [_to_out(o) for o in result.scalars().all()]


def _to_out(entry: OverrideLog) -> OverrideOut:
    return OverrideOut(
        id=entry.id,
        action=entry.action,
        target_type=entry.target_type,
        target_id=entry.target_id,
        requested_by=entry.requested_by,
        approved_by=entry.approved_by,
        reason=entry.reason,
        status=entry.status,
        created_at=entry.created_at.isoformat() if entry.created_at else "",
        approved_at=entry.approved_at.isoformat() if entry.approved_at else None,
    )
