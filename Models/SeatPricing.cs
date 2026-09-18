namespace CineBook.Models
{
    public static class SeatPricing
    {
        public static decimal GetPrice(PricingTier tier)
        {
            switch (tier)
            {
                case PricingTier.VIP:
                    return 20m;
                case PricingTier.Recliner:
                    return 18m;
                case PricingTier.Premium:
                    return 16m;
                case PricingTier.Regular:
                    return 12m;
                case PricingTier.Accessible:
                default:
                    return 10m;
            }
        }
    }
}