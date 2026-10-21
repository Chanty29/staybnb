namespace Staybnb.Services;

/// <summary>
/// One place for the price rule from the brief:
/// TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee.
/// The service fee is a percentage of the nightly subtotal.
/// </summary>
public static class BookingPricing
{
    public const decimal ServiceFeeRate = 0.12m;

    public static (decimal Subtotal, decimal ServiceFee, decimal Total) Calculate(
        decimal pricePerNight, decimal cleaningFee, int nights)
    {
        var subtotal = pricePerNight * nights;
        var serviceFee = Math.Round(subtotal * ServiceFeeRate, 2);
        var total = subtotal + cleaningFee + serviceFee;
        return (subtotal, serviceFee, total);
    }
}
