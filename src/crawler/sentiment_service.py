"""
Sentiment Analysis Microservice — QuantIQ Platform
Model: cardiffnlp/twitter-xlm-roberta-base-sentiment (multilingual, supports Vietnamese)

Start: uvicorn sentiment_service:app --host 0.0.0.0 --port 8001 --workers 1
"""

import os
import logging
from functools import lru_cache
from typing import Optional

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

# Lazy-load transformers to speed up import
_pipeline = None

logging.basicConfig(level=logging.INFO)
log = logging.getLogger(__name__)

MODEL_NAME = os.getenv(
    "SENTIMENT_MODEL",
    "cardiffnlp/twitter-xlm-roberta-base-sentiment"
)

app = FastAPI(
    title="QuantIQ Sentiment Service",
    description="Phân tích cảm xúc tin tức tài chính",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


# ── Model loading ───────────────────────────────────────────────────────────────
def get_pipeline():
    global _pipeline
    if _pipeline is None:
        from transformers import pipeline as hf_pipeline
        log.info(f"Loading model: {MODEL_NAME} (first load may take 1-2 min)...")
        _pipeline = hf_pipeline(
            "text-classification",
            model=MODEL_NAME,
            tokenizer=MODEL_NAME,
            top_k=1,
            truncation=True,
            max_length=512,
        )
        log.info("Model loaded ✓")
    return _pipeline


# Label map: model output → our standard labels
LABEL_MAP = {
    "positive": "positive",
    "negative": "negative",
    "neutral":  "neutral",
    # Some model variants use LABEL_0/1/2
    "LABEL_0":  "negative",
    "LABEL_1":  "neutral",
    "LABEL_2":  "positive",
}


# ── Schemas ─────────────────────────────────────────────────────────────────────
class AnalyzeRequest(BaseModel):
    text: str
    language: Optional[str] = "vi"


class AnalyzeResponse(BaseModel):
    label: str          # positive / negative / neutral
    score: float        # 0.0 - 1.0
    raw_label: str


class BatchRequest(BaseModel):
    texts: list[str]


# ── Routes ───────────────────────────────────────────────────────────────────────
@app.get("/health")
def health():
    return {"status": "ok", "model": MODEL_NAME}


@app.post("/analyze", response_model=AnalyzeResponse)
def analyze(req: AnalyzeRequest):
    if not req.text or not req.text.strip():
        raise HTTPException(status_code=400, detail="text is required")

    pipe = get_pipeline()
    try:
        results = pipe(req.text[:512])
        # top_k=1 returns [[{label, score}]]
        top = results[0] if isinstance(results[0], dict) else results[0][0]
        raw_label = top["label"]
        score     = round(float(top["score"]), 4)
        label     = LABEL_MAP.get(raw_label, "neutral")
        return AnalyzeResponse(label=label, score=score, raw_label=raw_label)
    except Exception as e:
        log.error(f"Inference error: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/analyze/batch")
def analyze_batch(req: BatchRequest):
    if not req.texts:
        return {"results": []}
    pipe = get_pipeline()
    results = []
    for text in req.texts[:50]:   # limit batch size
        try:
            r = pipe(text[:512])
            top = r[0] if isinstance(r[0], dict) else r[0][0]
            raw = top["label"]
            results.append({
                "label": LABEL_MAP.get(raw, "neutral"),
                "score": round(float(top["score"]), 4),
                "raw_label": raw,
            })
        except Exception:
            results.append({"label": "neutral", "score": 0.5, "raw_label": "error"})
    return {"results": results}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run("sentiment_service:app", host="0.0.0.0", port=8001, reload=False)
