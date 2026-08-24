using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Domain;

namespace Payments.Tests.Domain;

public sealed class TariffTests
{
    [Fact]
    public void Create_Should_UppercaseCurrency_And_TrimName()
    {
        var tariff = Tariff.Create(" Monthly A1 ", null, TariffKind.PerMonth, 100m, "usd", 0, 0, false);

        tariff.Name.ShouldBe("Monthly A1");
        tariff.Currency.ShouldBe("USD");
        tariff.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_Should_Throw_When_AmountNegative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Tariff.Create("Monthly", null, TariffKind.PerMonth, -1m, "USD", 0, 0, false));
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveFalse()
    {
        var tariff = Tariff.Create("Monthly", null, TariffKind.PerMonth, 100m, "USD", 0, 0, false);

        tariff.Deactivate();

        tariff.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Update_Should_Not_Change_Kind_Or_Currency()
    {
        var tariff = Tariff.Create("Monthly", null, TariffKind.PerMonth, 100m, "USD", 0, 0, false);

        tariff.Update("Monthly v2", null, 150m, 0, 0, true);

        tariff.Kind.ShouldBe(TariffKind.PerMonth);
        tariff.Currency.ShouldBe("USD");
        tariff.Amount.ShouldBe(150m);
        tariff.ChargeOnExcusedAbsence.ShouldBeTrue();
    }
}
