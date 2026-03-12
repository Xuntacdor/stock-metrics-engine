using System;
using System.Collections.Generic;

namespace backend_api.Api.Models;

public partial class RiskAlert
{
    public int AlertId { get; set; }

    public string UserId { get; set; } = null!;

    public string AlertType { get; set; } = null!;

    public decimal Rtt { get; set; }

    public string Message { get; set; } = null!;

    public bool IsAcknowledged { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
