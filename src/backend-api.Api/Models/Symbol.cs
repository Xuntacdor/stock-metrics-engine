using System;
using System.Collections.Generic;

namespace backend_api.Api.Models;

public partial class Symbol
{
    public string Symbol1 { get; set; } = null!;

    public string? CompanyName { get; set; }

    public string? Exchange { get; set; }

    public virtual ICollection<Candle> Candles { get; set; } = new List<Candle>();

    public virtual ICollection<MarginRatio> MarginRatios { get; set; } = new List<MarginRatio>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
}
