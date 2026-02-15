using System;
using System.Collections.Generic;

namespace backend_api.Api.Models;

public partial class Candle
{
    public string Symbol { get; set; } = null!;

    public long Timestamp { get; set; }

    public decimal? Open { get; set; }

    public decimal? High { get; set; }

    public decimal? Low { get; set; }

    public decimal? Close { get; set; }

    public long? Volume { get; set; }

    public virtual Symbol SymbolNavigation { get; set; } = null!;
}
