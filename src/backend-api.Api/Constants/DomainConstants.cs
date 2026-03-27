namespace backend_api.Api.Constants;

/// <summary>Order lifecycle statuses stored in Orders.Status.</summary>
public static class OrderStatus
{
    public const string Pending         = "PENDING";
    public const string Filled          = "FILLED";
    public const string PartiallyFilled = "PARTIALLY_FILLED";
    public const string Cancelled       = "CANCELLED";
    public const string Rejected        = "REJECTED";
}

/// <summary>Trade direction stored in Orders.Side.</summary>
public static class OrderSide
{
    public const string Buy  = "BUY";
    public const string Sell = "SELL";
}

/// <summary>Special order types beyond the standard LO/MP.</summary>
public static class OrderType
{
    public const string ForceSell = "FORCE_SELL";
}

/// <summary>Transaction types stored in Transactions.TransType.</summary>
public static class TransactionType
{
    public const string Buy       = "BUY";
    public const string Sell      = "SELL";
    public const string Deposit   = "DEPOSIT";
    public const string Withdraw  = "WITHDRAW";
    public const string Dividend  = "DIVIDEND";
    public const string ForceSell = "FORCE_SELL";
}

/// <summary>Alert kinds stored in PriceAlerts.AlertType.</summary>
public static class AlertType
{
    public const string Price  = "price";
    public const string Volume = "volume";
    public const string Rsi    = "rsi";
}

/// <summary>Comparison operators stored in PriceAlerts.Condition.</summary>
public static class AlertCondition
{
    public const string Gt  = "gt";
    public const string Gte = "gte";
    public const string Lt  = "lt";
    public const string Lte = "lte";
}

/// <summary>Risk alert types stored in RiskAlerts.AlertType.</summary>
public static class RiskAlertType
{
    public const string ForceSell  = "FORCE_SELL";
    public const string CallMargin = "CALL_MARGIN";
}
