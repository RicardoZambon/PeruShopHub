namespace PeruShopHub.Core.ValueObjects;

public record Money(decimal Amount, string Currency = "BRL")
{
    public static Money Zero => new(0m);
    public static Money FromBrl(decimal amount) => new(amount, "BRL");
}
