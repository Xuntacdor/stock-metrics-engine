namespace backend_api.Api.DTOs;

public record BuyingPowerResponse(decimal BuyingPower, decimal AvailableCash, decimal MarginValue);

public record RttResponse(decimal Rtt, decimal LoanAmount, bool IsAtRisk, string Status);

public record RiskAlertResponse(
    int AlertId,
    string AlertType,
    decimal Rtt,
    string Message,
    bool IsAcknowledged,
    DateTime CreatedAt);

public record SimulateLoanRequest(decimal LoanAmount);
