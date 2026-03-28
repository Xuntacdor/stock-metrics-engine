"""
Celery tasks — QuantIQ Crawler

Each task wraps the existing crawler logic with:
  - Automatic retry with exponential back-off (max 3 attempts)
  - Dead-letter routing after all retries are exhausted
  - Structured logging with task ID for traceability
"""

import logging
import time

from celery import Task
from celery.utils.log import get_task_logger
from celery_app import app

log = get_task_logger(__name__)


# ── Dead-letter sink ──────────────────────────────────────────────────────────
@app.task(queue="dead_letter", ignore_result=False)
def dead_letter_task(original_task: str, args: list, kwargs: dict, error: str) -> None:
    """
    Receives tasks that exhausted all retries.
    In production, plug in an alert (Slack, PagerDuty, email) here.
    """
    log.error(
        "DEAD LETTER | task=%s error=%s args=%s kwargs=%s",
        original_task, error, args, kwargs,
    )


# ── Base class with shared on_failure handler ─────────────────────────────────
class CrawlerTask(Task):
    abstract = True

    def on_failure(self, exc, task_id, args, kwargs, einfo):
        if self.request.retries >= self.max_retries:
            dead_letter_task.apply_async(
                kwargs={
                    "original_task": self.name,
                    "args": list(args),
                    "kwargs": kwargs,
                    "error": str(exc),
                },
                queue="dead_letter",
            )
        super().on_failure(exc, task_id, args, kwargs, einfo)


# ── Stock candle crawler ──────────────────────────────────────────────────────
@app.task(
    base=CrawlerTask,
    bind=True,
    name="tasks.crawl_stocks_task",
    max_retries=3,
    default_retry_delay=60,          # base delay; actual = 60 * 2^attempt (see autoretry_for)
    acks_late=True,
)
def crawl_stocks_task(self, symbols: list[str]) -> dict:
    """
    Fetch and persist OHLCV candles for the given symbol list.
    Retries up to 3 times with exponential back-off on any exception.
    """
    from crawler import fetch_and_save  # import here to keep startup fast

    log.info("[%s] Starting stock crawl for %d symbols", self.request.id, len(symbols))
    results = {"ok": [], "failed": []}

    for symbol in symbols:
        try:
            fetch_and_save(symbol)
            results["ok"].append(symbol)
            time.sleep(1)            # polite rate-limit
        except Exception as exc:
            log.warning("[%s] Failed to crawl %s: %s", self.request.id, symbol, exc)
            results["failed"].append(symbol)

    if results["failed"]:
        # Retry the whole task if any symbol failed
        failed = results["failed"]
        log.warning("[%s] %d symbols failed — scheduling retry", self.request.id, len(failed))
        try:
            raise self.retry(
                args=[failed],
                countdown=60 * (2 ** self.request.retries),
                exc=RuntimeError(f"Symbols failed: {failed}"),
            )
        except self.MaxRetriesExceededError:
            log.error("[%s] Max retries exceeded for symbols: %s", self.request.id, failed)

    log.info("[%s] Stock crawl complete — ok=%d failed=%d", self.request.id, len(results["ok"]), len(results["failed"]))
    return results


# ── News + sentiment crawler ──────────────────────────────────────────────────
@app.task(
    base=CrawlerTask,
    bind=True,
    name="tasks.crawl_news_task",
    max_retries=3,
    default_retry_delay=30,
    acks_late=True,
)
def crawl_news_task(self) -> dict:
    """
    Crawl RSS feeds, run sentiment analysis, and persist new articles.
    Retries up to 3 times with exponential back-off.
    """
    try:
        from news_crawler import main as run_news_crawler  # import here to keep startup fast

        log.info("[%s] Starting news crawl", self.request.id)
        run_news_crawler()
        log.info("[%s] News crawl complete", self.request.id)
        return {"status": "ok"}

    except Exception as exc:
        log.warning("[%s] News crawl failed: %s — retrying", self.request.id, exc)
        raise self.retry(
            countdown=30 * (2 ** self.request.retries),
            exc=exc,
        )
