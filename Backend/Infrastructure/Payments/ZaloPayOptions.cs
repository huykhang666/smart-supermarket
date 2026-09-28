namespace SmartSupermarket.Backend.Infrastructure.Payments;

public class ZaloPayOptions
{
    public const string SectionName = "ZaloPay";

    public string AppId { get; set; } = "2553";
    public string Key1 { get; set; } = "sdngBrRmqnhTaRvhhStgPcfrxUzCpmzE";
    public string Key2 { get; set; } = "trH0qqB8LioaKaEajAx6VHrxEFlConstraint";
    public string Endpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2/create";
    public string QueryEndpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2/query";
    public string CallbackUrl { get; set; } = "https://localhost:7198/api/v1/payments/zalo-pay/callback";
}
