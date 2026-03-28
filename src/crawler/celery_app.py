"""
Celery application — QuantIQ Crawler
Broker + backend: Redis (db 1, separate from the API's cache on db 0)

Start worker:  celery -A celery_app worker --loglevel=info -Q default,dead_letter
Start beat:    celery -A celery_app beat   --loglevel=info
"""

import os
from celery import Celery
from celery.schedules import crontab
from dotenv import load_dotenv
from pathlib import Path

load_dotenv(dotenv_path=Path(__file__).resolve().parents[2] / ".env")

REDIS_URL = os.getenv("REDIS_URL", "redis://localhost:6379/1")

app = Celery(
    "quantiq_crawler",
    broker=REDIS_URL,
    backend=REDIS_URL,
    include=["tasks"],
)

app.conf.update(
    # Serialisation
    task_serializer="json",
    result_serializer="json",
    accept_content=["json"],

    # Reliability: ACK only after the task body finishes; requeue on worker crash
    task_acks_late=True,
    task_reject_on_worker_lost=True,
    task_track_started=True,

    # Result TTL
    result_expires=3600,

    # Routing: failed tasks land in dead_letter after max_retries exhausted
    task_routes={
        "tasks.crawl_stocks_task": {"queue": "default"},
        "tasks.crawl_news_task":   {"queue": "default"},
        "tasks.dead_letter_task":  {"queue": "dead_letter"},
    },

    # Beat schedule
    beat_schedule={
        # Stock candle data — after market close (16:00 ICT = 09:00 UTC)
        "crawl-stocks-daily": {
            "task": "tasks.crawl_stocks_task",
            "schedule": crontab(hour=9, minute=0),
            "args": (
                [
                    "FPT", "VIC", "VHM", "VNM", "MWG",
                    "ACB", "TCB", "VCB", "BID", "CTG",
                    "HPG", "HSG", "MSN", "MBB", "STB",
                    "SSI", "VND", "HDB", "TPB", "LPB",
                ],
            ),
        },
        # News + sentiment — every 15 minutes during trading hours
        "crawl-news-every-15min": {
            "task": "tasks.crawl_news_task",
            "schedule": crontab(minute="*/15"),
        },
    },

    timezone="Asia/Ho_Chi_Minh",
    enable_utc=True,
)
