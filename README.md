# Stock Metrics Engine

A full-stack, containerized stock trading and investment analysis platform with real-time market data, AI-powered news sentiment, and portfolio management.

## Overview

This system combines a .NET 8 REST API, an Angular 17 frontend, and Python-based data pipeline services into a multi-container architecture. It targets the Vietnamese stock market (VN-Index, HNX) and integrates a HuggingFace NLP model for multilingual sentiment analysis of financial news.

## Architecture

```
stock-metrics-engine/
├── src/
│   ├── backend-api.Api/          # .NET 8 ASP.NET Core Web API
│   ├── backend-api.Api.Tests/    # xUnit unit tests
│   ├── crawler/                  # Python Celery workers + FastAPI sentiment service
│   └── frontend-web/             # Angular 17 standalone SPA
├── infra/
│   ├── nginx/                    # Reverse proxy & rate limiting
│   ├── sqlserver/                # SQL Server replication & backup scripts
│   ├── prometheus/               # Metrics scrape config
│   └── grafana/                  # Dashboard provisioning
├── .github/workflows/            # CI/CD pipelines
└── docker-compose.yml            # 14-container orchestration
```

**Key patterns:**
- Microservices: API, Crawler, Sentiment service, and Nginx each run as independent containers
- SQL Server primary/read-replica with automated backup sidecar
- Redis as both message broker (Celery) and application cache
- SignalR WebSocket hub for real-time price and alert push

## Tech Stack

| Layer | Technology |
|---|---|
| Backend API | .NET 8, ASP.NET Core, Entity Framework Core 8, SignalR |
| Frontend | Angular 17 (standalone), TailwindCSS, lightweight-charts |
| Database | SQL Server 2022 (primary + read replica) |
| Cache / Queue | Redis 7, Celery 5.4, Celery Beat |
| Sentiment NLP | FastAPI, HuggingFace Transformers (`cardiffnlp/twitter-xlm-roberta`) |
| Auth | JWT Bearer tokens, BCrypt password hashing |
| Payment | PayOS |
| Reverse Proxy | Nginx (Alpine) |
| Monitoring | Prometheus, Grafana, Serilog |
| Testing | xUnit, Jasmine/Karma, Playwright |
| CI/CD | GitHub Actions → GitHub Container Registry (GHCR) |

## Features

- **Real-time market data** — Live OHLC candle data and index tracking via SignalR WebSocket
- **Portfolio management** — Holdings, P&L, cash wallet, deposit/withdraw via PayOS
- **Order management** — Buy/sell order placement and execution
- **AI news sentiment** — Automated RSS crawling (CafeF and other Vietnamese sources) with multilingual sentiment scoring
- **Stock screener** — Filter by technical indicators (P/E, RSI, MA)
- **Price & risk alerts** — Threshold-based alerts and margin ratio monitoring via background worker
- **KYC verification** — Document upload integrated with FPT AI
- **Watchlist & leaderboard** — Social/gamification features
- **Observability** — Prometheus metrics, Grafana dashboards, structured Serilog logs, audit trail

## Getting Started

### Prerequisites

- Docker and Docker Compose
- (Optional, for local dev) .NET 8 SDK, Node.js 20, Python 3.11

### Run with Docker Compose

```bash
git clone https://github.com/Xuntacdor/stock-metrics-engine.git
cd stock-metrics-engine

# Copy and edit environment variables
cp .env .env.local
# Edit .env with your secrets (see Environment Variables section)

docker compose up -d
```

| Service | URL |
|---|---|
| Frontend / API (via Nginx) | http://localhost:80 |
| Swagger UI | http://localhost/swagger |
| Grafana | http://localhost:3000 (admin / admin) |
| Prometheus | http://localhost:9090 |
| SQL Server | localhost:1433 |
| Redis | localhost:6380 |

### Local Development

**Backend**
```bash
cd src/backend-api.Api
dotnet restore
dotnet run
# API on http://localhost:5000
```

**Frontend**
```bash
cd src/frontend-web
npm install
npm start
# Dev server with HMR on http://localhost:4200
```

**Database migrations**
```bash
cd src/backend-api.Api
dotnet ef database update
# Add a new migration:
dotnet ef migrations add <MigrationName>
```

## Testing

**Backend (xUnit)**
```bash
cd src/backend-api.Api.Tests
dotnet test
```

**Frontend (Karma + Jasmine)**
```bash
cd src/frontend-web
npm test
```

**E2E (Playwright)**
```bash
cd src/frontend-web
npm run e2e
npm run e2e:ui   # Interactive UI mode
```

**Python linting**
```bash
cd src/crawler
flake8 . --max-line-length=120
```

## Environment Variables

Key variables expected in `.env` (at project root):

```bash
# Database
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=QuantIQ_DB;User Id=sa;Password=<password>;TrustServerCertificate=True;
DB_PASSWORD=<password>

# JWT
JwtSettings__SecretKey=<32+ character secret>
JwtSettings__Issuer=QuantIQ
JwtSettings__Audience=QuantIQApp
JwtSettings__ExpiresInMinutes=60

# Redis
Redis__ConnectionString=redis:6379

# External integrations
FptAi__ApiKey=<FPT AI key for KYC>
PayOS__ClientId=<PayOS client ID>
PayOS__ApiKey=<PayOS API key>
PayOS__ChecksumKey=<PayOS checksum key>

# Email (SMTP)
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__User=<email>
Smtp__Password=<app password>

# Seed
AdminSeed__Password=<admin password>
```

## CI/CD

Two GitHub Actions workflows run on push to `main` or `develop`:

- **ci.yml** — Restores, builds, and tests the API (.NET 8), lints and builds the frontend (Node 20), lints Python (flake8), and validates `docker-compose.yml`
- **docker-build.yml** — Builds and pushes multi-stage Docker images for `backend-api`, `crawler`, and `sentiment` to GHCR, tagged with the commit SHA and `latest`

## License

See [LICENSE](LICENSE).
