namespace localmarket_preordersystem.Application.Common.Models
{
    /// <summary>Az időjárás-szolgáltatás nyers napi előrejelzése (a magyar leírást az Application állítja elő).</summary>
    public sealed record DailyForecast(
        DateTime Date,
        double TemperatureMinC,
        double TemperatureMaxC,
        double PrecipitationMm,
        int WeatherCode);
}
