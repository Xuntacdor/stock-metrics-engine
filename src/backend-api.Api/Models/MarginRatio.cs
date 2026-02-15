using System;
using System.Collections.Generic;

namespace backend_api.Api.Models;

public partial class MarginRatio
{
    public int RatioId { get; set; }

    public string Symbol { get; set; } = null!;

    public decimal InitialRate { get; set; }

    public decimal MaintenanceRate { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? ExpiredDate { get; set; }

    public virtual Symbol SymbolNavigation { get; set; } = null!;
}
