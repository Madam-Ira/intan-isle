import asyncio
import logging
from typing import Optional

from aiokafka import AIOKafkaProducer

from app.core.config import settings

logger = logging.getLogger(__name__)

_producer: Optional[AIOKafkaProducer] = None


async def init_kafka() -> None:
    global _producer
    try:
        _producer = AIOKafkaProducer(
            bootstrap_servers=settings.KAFKA_BOOTSTRAP_SERVERS,
            request_timeout_ms=3000,
            metadata_max_age_ms=3000,
        )
        await asyncio.wait_for(_producer.start(), timeout=5.0)
        logger.info("Kafka producer connected at %s", settings.KAFKA_BOOTSTRAP_SERVERS)
    except Exception as exc:
        logger.warning("Kafka unavailable (%s) — event streaming disabled.", exc)
        _producer = None


async def close_kafka() -> None:
    global _producer
    if _producer is not None:
        await _producer.stop()
        _producer = None


async def get_producer() -> Optional[AIOKafkaProducer]:
    return _producer
