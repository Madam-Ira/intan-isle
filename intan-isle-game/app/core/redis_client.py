import logging
from typing import Optional

import redis.asyncio as aioredis

from app.core.config import settings

logger = logging.getLogger(__name__)

_redis: Optional[aioredis.Redis] = None


async def init_redis() -> None:
    global _redis
    try:
        _redis = aioredis.from_url(settings.REDIS_URL, decode_responses=True)
        await _redis.ping()
        logger.info("Redis connected at %s", settings.REDIS_URL)
    except Exception as exc:
        logger.warning("Redis unavailable (%s) — cache features disabled.", exc)
        _redis = None


async def close_redis() -> None:
    global _redis
    if _redis is not None:
        await _redis.close()
        _redis = None


async def get_redis() -> Optional[aioredis.Redis]:
    return _redis
