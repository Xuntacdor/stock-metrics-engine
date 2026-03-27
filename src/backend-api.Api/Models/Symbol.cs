using System;
using System.Collections.Generic;

namespace backend_api.Api.Models;

public partial class Symbol
{
    public string Symbol1 { get; set; } = null!;

    public string? CompanyName { get; set; }

    public string? Exchange { get; set; }

    public string? Sector { get; set; }

    public decimal? Pe { get; set; }

    /// <summary>Market capitalisation in billions VND.</summary>
    public decimal? MarketCap { get; set; }

    public virtual ICollection<Candle> Candles { get; set; } = new List<Candle>();

    public virtual ICollection<MarginRatio> MarginRatios { get; set; } = new List<MarginRatio>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();

    public virtual ICollection<CorporateAction> CorporateActions { get; set; } = new List<CorporateAction>();
}
