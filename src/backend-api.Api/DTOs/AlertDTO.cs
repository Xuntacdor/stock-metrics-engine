using System.ComponentModel.DataAnnotations;

namespace backend_api.Api.DTOs;

public record CreateAlertRequest(
    [Required] string Symbol,
    [Required] string AlertType,   // price | volume | rsi | news
    [Required] string Condition,   // gt | gte | lt | lte
    decimal ThresholdValue,
    bool NotifyOnce = true
);

public record UpdateAlertRequest(bool IsActive);

public record AlertResponse(
    int AlertId,
    string Symbol,
    string AlertType,
    string Condition,
    decimal ThresholdValue,
    bool IsActive,
    bool IsTriggered,
    bool NotifyOnce,
    DateTime CreatedAt,
    DateTime? TriggeredAt
);

/// <summary>Payload pushed to the client via SignalR when an alert fires.</summary>
public record AlertTriggeredNotification(
    int AlertId,
    string Symbol,
    string AlertType,
    string Condition,
    decimal ThresholdValue,
    decimal CurrentValue,
    DateTime TriggeredAt
);
