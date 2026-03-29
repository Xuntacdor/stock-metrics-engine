namespace backend_api.Api.DTOs;

public class SymbolDto{
    public string Symbol { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? Exchange { get; set; }
}

public class CreateSymbolRequest
{
    public string Symbol { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? Exchange { get; set; }
}

public class CandleDto
{
    public long Timestamp { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Close { get; set; }
    public long? Volume { get; set; }
}
