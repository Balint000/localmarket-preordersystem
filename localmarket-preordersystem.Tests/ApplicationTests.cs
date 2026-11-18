using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Time;
using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Tests;

public class ValidationBuilderTests
{
    [Fact]
    public void Valid_input_does_not_throw()
    {
        new ValidationBuilder()
            .Required("name", "Alma", "kötelező")
            .Email("email", "a@b.hu")
            .Positive("price", 10m, "pozitív")
            .NonNegative("qty", 0m, "nem negatív")
            .ThrowIfInvalid();
    }

    [Fact]
    public void Errors_are_collected_per_field()
    {
        var ex = Assert.Throws<ValidationException>(() => new ValidationBuilder()
            .Required("name", " ", "A név kötelező.")
            .Email("email", "nem-email")
            .NonNegative("qty", -1m, "Nem lehet negatív.")
            .MaxLength("note", new string('x', 11), 10)
            .ThrowIfInvalid());

        Assert.Contains("name", ex.Errors.Keys);
        Assert.Contains("email", ex.Errors.Keys);
        Assert.Contains("qty", ex.Errors.Keys);
        Assert.Contains("note", ex.Errors.Keys);
    }

    [Fact]
    public void Zero_is_not_positive_but_is_non_negative()
    {
        Assert.False(new ValidationBuilder().Positive("x", 0m, "m").IsValid);
        Assert.True(new ValidationBuilder().NonNegative("x", 0m, "m").IsValid);
    }
}

public class ZonedTimeProviderTests
{
    [Fact]
    public void Configured_zone_is_used_for_local_time()
    {
        var provider = ZonedTimeProvider.Create("Europe/Budapest");
        var winter = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var summer = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(TimeSpan.FromHours(1), TimeZoneInfo.ConvertTime(winter, provider.LocalTimeZone).Offset);
        Assert.Equal(TimeSpan.FromHours(2), TimeZoneInfo.ConvertTime(summer, provider.LocalTimeZone).Offset);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Nincs/Ilyen_Zona")]
    public void Unknown_or_missing_zone_falls_back_without_throwing(string? id)
    {
        var provider = ZonedTimeProvider.Create(id);
        Assert.NotNull(provider.LocalTimeZone);
    }
}
