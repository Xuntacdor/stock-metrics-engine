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
