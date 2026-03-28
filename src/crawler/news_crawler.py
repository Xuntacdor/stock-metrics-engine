"""
News Crawler — QuantIQ Platform
Crawl RSS từ CafeF & Tin nhanh chứng khoán, phân tích sentiment, lưu SQL Server.

Chạy thủ công: python news_crawler.py
Chạy tự động:  Celery beat (celery_app.py) — task 'crawl_news_task' mỗi 15 phút.
"""

import os
import re
import time
import logging
import requests
import pyodbc
import feedparser
from datetime import datetime, timezone
from pathlib import Path
from dotenv import load_dotenv

# Load .env from repo root (two levels up from this file)
env_path = Path(__file__).resolve().parents[2] / ".env"
load_dotenv(dotenv_path=env_path)
logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

# ── Config ─────────────────────────────────────────────────────────────────────
# Try to read individual vars first; fall back to parsing ConnectionStrings__DefaultConnection


def _parse_conn_string():
    """Parse 'Server=...;Database=...;User Id=...;Password=...;' style connection string."""
    conn_str = os.getenv("ConnectionStrings__DefaultConnection", "")
    parsed = {}
    for part in conn_str.split(";"):
        if "=" in part:
            k, _, v = part.partition("=")
            parsed[k.strip().lower()] = v.strip()
    return parsed


_cs = _parse_conn_string()

DB_SERVER = os.getenv("DB_SERVER") or _cs.get("server", "localhost").split(",")[0]
DB_PORT = "1433"
DB_NAME = os.getenv("DB_NAME") or _cs.get("database", "QuantIQ_DB")
DB_USER = os.getenv("DB_USER") or _cs.get("user id", "sa")
DB_PASSWORD = os.getenv("DB_PASSWORD") or _cs.get("password", "")

SENTIMENT_URL = os.getenv("SENTIMENT_SERVICE_URL", "http://localhost:8001/analyze")

# RSS feeds: (url, default_source)
RSS_FEEDS = [
    ("https://cafef.vn/chung-khoan.rss",           "cafef"),
    ("https://cafef.vn/tai-chinh-ngan-hang.rss",    "cafef"),
    ("https://tinnhanhchungkhoan.vn/rss/all.rss",   "tinnhanhchungkhoan"),
    ("https://vneconomy.vn/chung-khoan.rss",        "vneconomy"),
]

# Danh sách mã CK phổ biến (sẽ detect trong title/summary)
WATCHLIST = [
    "FPT", "VIC", "VHM", "VNM", "MWG", "ACB", "TCB", "VCB", "BID", "CTG",
    "HPG", "HSG", "MSN", "MBB", "STB", "SSI", "VND", "HDB", "TPB", "LPB",
    "VRE", "GAS", "PLX", "PNJ", "REE", "SHB", "EIB", "VCI", "DGC", "DCM",
]

# ── Database ────────────────────────────────────────────────────────────────────


def get_conn():
    conn_str = (
        f"DRIVER={{ODBC Driver 18 for SQL Server}};"
        f"SERVER={DB_SERVER},{DB_PORT};"
        f"DATABASE={DB_NAME};"
        f"UID={DB_USER};PWD={DB_PASSWORD};"
        "TrustServerCertificate=yes;"
    )
    log.debug(f"Connecting: SERVER={DB_SERVER} DATABASE={DB_NAME} UID={DB_USER}")
    return pyodbc.connect(conn_str)


def article_exists(cursor, url: str) -> bool:
    cursor.execute("SELECT 1 FROM NewsArticles WHERE Url = ?", url)
    return cursor.fetchone() is not None


def insert_article(cursor, article: dict) -> None:
    sql = """
        INSERT INTO NewsArticles
            (Symbol, Title, Url, Source, Summary, PublishedAt, Sentiment, SentimentScore)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    """
    cursor.execute(
        sql,
        article.get("symbol"),
        article["title"][:500],
        article["url"][:1000],
        article.get("source"),
        article.get("summary", "")[:2000],
        article.get("published_at"),
        article.get("sentiment"),
        article.get("sentiment_score"),
    )


# ── Sentiment ───────────────────────────────────────────────────────────────────
def analyze_sentiment(text: str) -> tuple[str, float]:
    """Gọi FastAPI sentiment service. Fallback về neutral nếu lỗi."""
    try:
        resp = requests.post(SENTIMENT_URL, json={"text": text[:512]}, timeout=5)
        if resp.ok:
            data = resp.json()
            return data.get("label", "neutral"), float(data.get("score", 0.5))
    except Exception as e:
        log.warning(f"Sentiment service unavailable: {e}")
    return "neutral", 0.5


# ── Symbol Detection ────────────────────────────────────────────────────────────
def detect_symbol(text: str) -> str | None:
    """Tìm mã CK đầu tiên xuất hiện trong văn bản."""
    upper = text.upper()
    for sym in WATCHLIST:
        # khớp từ đơn (không phải substring của từ dài hơn)
        if re.search(rf"\b{sym}\b", upper):
            return sym
    return None


# ── RSS Parsing ─────────────────────────────────────────────────────────────────
def parse_date(entry) -> datetime | None:
    ts = entry.get("published_parsed") or entry.get("updated_parsed")
    if ts:
        return datetime(*ts[:6], tzinfo=timezone.utc)
    return datetime.now(timezone.utc)


def crawl_feed(feed_url: str, source: str, cursor) -> int:
    log.info(f"Crawling {source}: {feed_url}")
    try:
        feed = feedparser.parse(feed_url)
    except Exception as e:
        log.error(f"Failed to parse {feed_url}: {e}")
        return 0

    saved = 0
    for entry in feed.entries:
        url = entry.get("link", "").strip()
        title = entry.get("title", "").strip()
        if not url or not title:
            continue
        if article_exists(cursor, url):
            continue

        summary = entry.get("summary", "").strip()
        # Strip HTML tags
        summary = re.sub(r"<[^>]+>", "", summary)

        full_text = f"{title}. {summary}"
        symbol = detect_symbol(full_text)
        sentiment, score = analyze_sentiment(full_text)
        pub_date = parse_date(entry)

        article = {
            "symbol": symbol,
            "title": title,
            "url": url,
            "source": source,
            "summary": summary[:2000],
            "published_at": pub_date,
            "sentiment": sentiment,
            "sentiment_score": score,
        }

        try:
            insert_article(cursor, article)
            saved += 1
            log.info(f"  + [{sentiment:8s}|{score:.2f}] {title[:70]}")
        except Exception as e:
            log.error(f"  ! Insert failed: {e}")

        time.sleep(0.2)   # rate-limit sentiment service

    return saved


# ── Main ────────────────────────────────────────────────────────────────────────
def main():
    log.info("=== QuantIQ News Crawler started ===")
    conn = get_conn()
    cursor = conn.cursor()
    total = 0

    for url, source in RSS_FEEDS:
        saved = crawl_feed(url, source, cursor)
        total += saved
        conn.commit()
        time.sleep(1)

    conn.close()
    log.info(f"=== Done. Saved {total} new articles ===")


if __name__ == "__main__":
    main()
