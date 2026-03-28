using Prometheus;

namespace backend_api.Api.Metrics;

/// <summary>
/// All application-level Prometheus metrics, defined as static singletons so they
/// can be incremented from any layer without DI ceremony.
///
/// Exposed at GET /metrics (already mapped in Program.cs via app.MapMetrics()).
/// </summary>
public static class AppMetrics
{
    // ── Orders ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Total orders placed, labelled by side (BUY/SELL) and outcome (FILLED/CANCELLED/REJECTED).
    /// Derive orders-per-minute in Prometheus: rate(quantiq_orders_total[1m])
    /// </summary>
    public static readonly Counter OrdersTotal = Prometheus.Metrics.CreateCounter(
        "quantiq_orders_total",
        "Total number of orders submitted to the matching engine.",
        new CounterConfiguration
        {
            LabelNames = ["side", "outcome"]   // side: BUY|SELL, outcome: FILLED|CANCELLED|REJECTED
        });

    /// <summary>Order processing latency in milliseconds.</summary>
    public static readonly Histogram OrderLatencyMs = Prometheus.Metrics.CreateHistogram(
        "quantiq_order_latency_ms",
        "End-to-end order placement latency in milliseconds.",
        new HistogramConfiguration
        {
            LabelNames = ["side"],
            Buckets    = [10, 25, 50, 100, 250, 500, 1000, 2500]
        });

    // ── SignalR / WebSocket connections ───────────────────────────────────────

    /// <summary>Number of currently active SignalR hub connections.</summary>
    public static readonly Gauge SignalRConnections = Prometheus.Metrics.CreateGauge(
        "quantiq_signalr_connections_active",
        "Number of currently active SignalR WebSocket connections.");

    // ── Portfolio P&L distribution ────────────────────────────────────────────

    /// <summary>
    /// Distribution of unrealised P&L values across the user base (VND).
    /// Recorded by the PortfolioPnLWorker every 5 minutes.
    /// Useful for risk/business dashboards — not per-user, just a population histogram.
    /// </summary>
    public static readonly Histogram PortfolioPnLVnd = Prometheus.Metrics.CreateHistogram(
        "quantiq_portfolio_pnl_vnd",
        "Distribution of unrealised portfolio P&L values in VND.",
        new HistogramConfiguration
        {
            Buckets = [-50_000_000, -10_000_000, -5_000_000, -1_000_000,
                         0, 1_000_000, 5_000_000, 10_000_000, 50_000_000, 100_000_000]
        });

    // ── Business activity counters ────────────────────────────────────────────

    /// <summary>Total KYC documents submitted, by final outcome (APPROVED/REJECTED/PENDING).</summary>
    public static readonly Counter KycTotal = Prometheus.Metrics.CreateCounter(
        "quantiq_kyc_total",
        "Total KYC submissions.",
        new CounterConfiguration { LabelNames = ["status"] });

    /// <summary>Total deposits initiated and their payment outcome.</summary>
    public static readonly Counter DepositsTotal = Prometheus.Metrics.CreateCounter(
        "quantiq_deposits_total",
        "Total deposit requests initiated.",
        new CounterConfiguration { LabelNames = ["status"] }); // PAID | CANCELLED | EXPIRED
}
