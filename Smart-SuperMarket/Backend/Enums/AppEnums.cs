namespace SmartSupermarket.Backend.Enums;

public enum UserRole
{
    Admin = 1,
    Manager = 2,
    Staff = 3,
    Customer = 4
}

public enum OrderStatus
{
    Completed = 1,
    Cancelled = 2
}

public enum PaymentMethod
{
    Cash = 1,
    QRCode = 2,
    CreditCard = 3
}

public enum ReportType
{
    Revenue = 1,
    Forecast = 2,
    Suggestion = 3,
    Analysis = 4
}
