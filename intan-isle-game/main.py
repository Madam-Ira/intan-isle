import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.core.config import settings
from app.core.database import close_db, init_db
from app.core.kafka_producer import close_kafka, init_kafka
from app.core.redis_client import close_redis, init_redis
from app.routers import auth, dashboard, game_bridge, overrides, sensors

logging.basicConfig(level=settings.LOG_LEVEL, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(application: FastAPI):
    logger.info("Starting %s (%s)", settings.APP_NAME, settings.APP_ENV)

    # Startup — each service degrades gracefully if unavailable
    try:
        await init_db()
        logger.info("Database connected and tables ensured.")
    except Exception as exc:
        logger.warning("Database unavailable (%s) — DB endpoints will fail.", exc)

    await init_redis()
    await init_kafka()

    logger.info("%s ready.", settings.APP_NAME)
    yield

    # Shutdown
    await close_kafka()
    await close_redis()
    await close_db()
    logger.info("%s shut down.", settings.APP_NAME)


app = FastAPI(
    title=settings.APP_NAME,
    version="0.1.0",
    lifespan=lifespan,
)

app.include_router(auth.router)
app.include_router(sensors.router)
app.include_router(dashboard.router)
app.include_router(game_bridge.router)
app.include_router(overrides.router)


@app.get("/", tags=["root"])
async def root():
    return {
        "service": settings.APP_NAME,
        "env": settings.APP_ENV,
        "game_bridge_stub": settings.GAME_BRIDGE_STUB_MODE,
    }
