import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface NewsArticle {
    articleId: number;
    symbol: string | null;
    title: string;
    url: string;
    source: string | null;
    summary: string | null;
    publishedAt: string | null;
    sentiment: 'positive' | 'negative' | 'neutral' | null;
    sentimentScore: number | null;
}

export interface SentimentSummary {
    symbol: string;
    total: number;
    bullish: number;
    bearish: number;
    neutral: number;
    bullishPct: number;
    bearishPct: number;
    neutralPct: number;
    overallSignal: 'BULLISH' | 'BEARISH' | 'NEUTRAL';
}

export interface NewsComment {
    commentId: number;
    symbol: string;
    userId: string;
    username: string;
    content: string;
    createdAt: string;
}

export interface SentimentDay {
    date: string;        // "YYYY-MM-DD"
    total: number;
    bullish: number;
    bearish: number;
    neutral: number;
    avgScore: number;    // 0.0 – 1.0 model confidence
    signal: 'BULLISH' | 'BEARISH' | 'NEUTRAL';
}

export interface LeaderboardEntry {
    rank: number;
    userId: string;
    username: string;
    realizedPnL: number;
    realizedPnLPct: number;
    tradeCount: number;
}

@Injectable({ providedIn: 'root' })
export class NewsService {
    private readonly http = inject(HttpClient);
    private readonly base = environment.apiUrl;

    getNews(symbol?: string, limit = 20): Observable<NewsArticle[]> {
        const params: any = { limit };
        if (symbol) params['symbol'] = symbol;
        return this.http.get<NewsArticle[]>(`${this.base}/news`, { params });
    }

    getSentimentSummary(symbol: string, days = 7): Observable<SentimentSummary> {
        return this.http.get<SentimentSummary>(`${this.base}/news/sentiment`, {
            params: { symbol, days },
        });
    }

    /**
     * GET /api/news/sentiment/trend?symbol=FPT&days=30
     * Returns daily aggregated sentiment scores ordered by date ascending.
     * Use for rendering a sentiment trend chart on the stock detail page.
     */
    getSentimentTrend(symbol: string, days = 30): Observable<SentimentDay[]> {
        return this.http.get<SentimentDay[]>(`${this.base}/news/sentiment/trend`, {
            params: { symbol, days },
        });
    }

    getComments(symbol: string): Observable<NewsComment[]> {
        return this.http.get<NewsComment[]>(`${this.base}/comments/${symbol}`);
    }

    postComment(symbol: string, content: string): Observable<NewsComment> {
        return this.http.post<NewsComment>(`${this.base}/comments/${symbol}`, { content });
    }

    deleteComment(commentId: number): Observable<void> {
        return this.http.delete<void>(`${this.base}/comments/${commentId}`);
    }

    getLeaderboard(limit = 20): Observable<LeaderboardEntry[]> {
        return this.http.get<LeaderboardEntry[]>(`${this.base}/leaderboard`, { params: { limit } });
    }
}
