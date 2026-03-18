from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """NurAIN backend configuration. Reads from environment / .env file."""

    APP_NAME: str = "NurAIN-MVP"
    APP_ENV: str = "development"
    LOG_LEVEL: str = "INFO"

    # ── Database (PostgreSQL + asyncpg) ──────────────────────────────
    DATABASE_URL: str = "postgresql+asyncpg://nurain:nurain_local@localhost:5432/nurain_db"

    # ── Redis ────────────────────────────────────────────────────────
    REDIS_URL: str = "redis://localhost:6379/0"

    # ── Kafka ────────────────────────────────────────────────────────
    KAFKA_BOOTSTRAP_SERVERS: str = "localhost:9092"

    # ── JWT ───────────────────────────────────────────────────────────
    JWT_SECRET_KEY: str = "local-dev-secret-change-in-production"
    JWT_ALGORITHM: str = "HS256"
    JWT_ACCESS_TOKEN_EXPIRE_MINUTES: int = 30
    JWT_REFRESH_TOKEN_EXPIRE_MINUTES: int = 10080  # 7 days

    # ── Game bridge ──────────────────────────────────────────────────
    GAME_BRIDGE_STUB_MODE: bool = True

    model_config = {"env_file": ".env", "env_file_encoding": "utf-8", "extra": "ignore"}


settings = Settings()
