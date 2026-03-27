namespace backend_api.Api.DTOs;

public class ScreenerFilterRequest
{
    public decimal PriceMin { get; set; } = 0;
    public decimal PriceMax { get; set; } = decimal.MaxValue;

    public decimal PeMin { get; set; } = 0;
    public decimal PeMax { get; set; } = decimal.MaxValue;

    public decimal RsiMin { get; set; } = 0;
    public decimal RsiMax { get; set; } = 100;

    public decimal VolumeMin { get; set; } = 0;

    /// <summary>all | large | mid | small</summary>
    public string MarketCap { get; set; } = "all";

    /// <summary>Comma-separated list of sectors to include, empty = all.</summary>
    public string Sector { get; set; } = string.Empty;

    /// <summary>changePct | rsi | pe | marketCap | volume</summary>
    public string SortBy { get; set; } = "changePct";

    public bool SortDesc { get; set; } = true;

    public int Limit { get; set; } = 50;
}

public record ScreenerResultDto(
    string Symbol,
    string? CompanyName,
    string? Exchange,
    string? Sector,
    decimal? LastClose,
    decimal? PrevClose,
    decimal ChangePct,
    long? Volume,
    decimal? Pe,
    decimal? MarketCap,
    decimal? Rsi14
);
