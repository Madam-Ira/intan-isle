# ────────────────────────────────────────────────────────────────────────
#  TRADE SECRET PROTECTED — STUB MODE ONLY
#
#  The NurAIN game bridge protocol, payload schemas, AI algorithm
#  integration, and real-time sync logic are IP-protected under NDA.
#
#  This file provides STUB endpoints that log calls and return mock
#  responses so the frontend (Unity client) and dashboard can develop
#  and test against a stable contract.
#
#  GAME_BRIDGE_STUB_MODE must remain True in this repository. The
#  production bridge is maintained in a private repository and deployed
#  separately. Do not implement real game logic here.
# ────────────────────────────────────────────────────────────────────────

import logging
from typing import Optional

from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel, Field

from app.core.config import settings

logger = logging.getLogger(__name__)

GAME_BRIDGE_STUB_MODE = settings.GAME_BRIDGE_STUB_MODE

router = APIRouter(prefix="/game", tags=["game-bridge"])


# ── Schemas ────────────────────────────────────────────────────────────

class BlessingUpdateRequest(BaseModel):
    player_id: str = Field(min_length=1, max_length=200)
    delta: float
    reason: str = Field(min_length=1, max_length=500)


class BlessingUpdateResponse(BaseModel):
    player_id: str
    new_score: float
    veil_access_granted: bool
    stub: bool


class CurriculumSyncRequest(BaseModel):
    player_id: str = Field(min_length=1, max_length=200)
    pillar_id: str = Field(min_length=1, max_length=100)
    competency_delta: float
    context: Optional[str] = Field(None, max_length=1000)


class CurriculumSyncResponse(BaseModel):
    player_id: str
    pillar_id: str
    accepted: bool
    stub: bool


class DistressSignalRequest(BaseModel):
    player_id: str = Field(min_length=1, max_length=200)
    signal_tier: str = Field(min_length=1, max_length=50)
    signal_type: str = Field(min_length=1, max_length=100)
    session_context: Optional[str] = Field(None, max_length=1000)


class DistressSignalResponse(BaseModel):
    player_id: str
    acknowledged: bool
    stub: bool


# ── Guard ──────────────────────────────────────────────────────────────

def _assert_stub() -> None:
    if not GAME_BRIDGE_STUB_MODE:
        raise HTTPException(
            status_code=status.HTTP_501_NOT_IMPLEMENTED,
            detail="Production game bridge is deployed separately. This instance is stub-only.",
        )


# ── Endpoints ──────────────────────────────────────────────────────────

@router.post("/blessing", response_model=BlessingUpdateResponse)
async def blessing_update(body: BlessingUpdateRequest):
    _assert_stub()
    logger.info("[STUB] Blessing update — player=%s delta=%.4f reason=%s", body.player_id, body.delta, body.reason)
    return BlessingUpdateResponse(
        player_id=body.player_id,
        new_score=50.0,
        veil_access_granted=False,
        stub=True,
    )


@router.post("/curriculum", response_model=CurriculumSyncResponse)
async def curriculum_sync(body: CurriculumSyncRequest):
    _assert_stub()
    logger.info(
        "[STUB] Curriculum sync — player=%s pillar=%s delta=%.4f",
        body.player_id,
        body.pillar_id,
        body.competency_delta,
    )
    return CurriculumSyncResponse(
        player_id=body.player_id,
        pillar_id=body.pillar_id,
        accepted=True,
        stub=True,
    )


@router.post("/distress", response_model=DistressSignalResponse)
async def distress_signal(body: DistressSignalRequest):
    _assert_stub()
    logger.info(
        "[STUB] Distress signal — player=%s tier=%s type=%s",
        body.player_id,
        body.signal_tier,
        body.signal_type,
    )
    return DistressSignalResponse(
        player_id=body.player_id,
        acknowledged=True,
        stub=True,
    )
