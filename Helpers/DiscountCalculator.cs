namespace HotelManagement.Helpers
{
    public class DiscountCalculator
    {
        private const int LoyaltyPointsPerDollar = 10;
        private const decimal LoyaltyDiscountRate = 0.01m; // 1% per 100 points
        private const int PointsEarnedPerDollar = 5;

        public decimal CalculateDiscount(decimal amount, decimal discountPercentage, decimal maxDiscountAmount)
        {
            var discount = amount * (discountPercentage / 100);

            if (maxDiscountAmount > 0 && discount > maxDiscountAmount)
            {
                discount = maxDiscountAmount;
            }

            return Math.Round(discount, 2);
        }

        public decimal CalculateLoyaltyDiscount(int loyaltyPoints)
        {
            // 100 points = 1% discount, max 20% discount
            var discountPercentage = Math.Min((loyaltyPoints / 100) * LoyaltyDiscountRate, 0.20m);
            return discountPercentage;
        }

        public int CalculateLoyaltyPointsEarned(decimal amount)
        {
            // Earn 5 points per dollar spent
            return (int)(amount * PointsEarnedPerDollar);
        }

        public decimal CalculateSeasonalDiscount(DateTime checkInDate, decimal basePrice)
        {
            var season = GetSeason(checkInDate);
            decimal discountPercentage = 0;

            switch (season)
            {
                case "Winter":
                    discountPercentage = 0.15m; // 15% winter discount
                    break;
                case "Summer":
                    discountPercentage = 0.10m; // 10% summer discount
                    break;
                case "Spring":
                case "Fall":
                    discountPercentage = 0.05m; // 5% spring/fall discount
                    break;
            }

            return basePrice * discountPercentage;
        }

        public decimal CalculateEarlyBirdDiscount(DateTime bookingDate, DateTime checkInDate, decimal basePrice)
        {
            var daysInAdvance = (checkInDate - bookingDate).Days;

            if (daysInAdvance >= 30)
                return basePrice * 0.20m; // 20% off for 30+ days advance
            else if (daysInAdvance >= 14)
                return basePrice * 0.10m; // 10% off for 14+ days advance
            else if (daysInAdvance >= 7)
                return basePrice * 0.05m; // 5% off for 7+ days advance

            return 0;
        }

        public decimal CalculateLastMinuteDiscount(DateTime checkInDate, decimal basePrice)
        {
            var daysUntilCheckIn = (checkInDate - DateTime.UtcNow).Days;

            if (daysUntilCheckIn <= 1)
                return basePrice * 0.25m; // 25% off for last minute
            else if (daysUntilCheckIn <= 3)
                return basePrice * 0.15m; // 15% off for within 3 days
            else if (daysUntilCheckIn <= 7)
                return basePrice * 0.10m; // 10% off for within 7 days

            return 0;
        }

        public decimal CalculateWeeklyStayDiscount(int numberOfNights, decimal totalPrice)
        {
            if (numberOfNights >= 7)
                return totalPrice * 0.10m; // 10% off for weekly stays
            else if (numberOfNights >= 3)
                return totalPrice * 0.05m; // 5% off for 3+ nights

            return 0;
        }

        public decimal CalculateBulkBookingDiscount(int numberOfRooms, decimal totalPrice)
        {
            if (numberOfRooms >= 5)
                return totalPrice * 0.15m; // 15% off for 5+ rooms
            else if (numberOfRooms >= 3)
                return totalPrice * 0.10m; // 10% off for 3+ rooms

            return 0;
        }

        public decimal CalculateBestAvailableDiscount(
            DateTime bookingDate,
            DateTime checkInDate,
            int numberOfNights,
            int numberOfRooms,
            decimal totalPrice,
            int loyaltyPoints)
        {
            var discounts = new List<decimal>
            {
                CalculateEarlyBirdDiscount(bookingDate, checkInDate, totalPrice),
                CalculateLastMinuteDiscount(checkInDate, totalPrice),
                CalculateWeeklyStayDiscount(numberOfNights, totalPrice),
                CalculateBulkBookingDiscount(numberOfRooms, totalPrice),
                totalPrice * CalculateLoyaltyDiscount(loyaltyPoints)
            };

            return discounts.Max();
        }

        private string GetSeason(DateTime date)
        {
            var month = date.Month;
            if (month >= 3 && month <= 5) return "Spring";
            if (month >= 6 && month <= 8) return "Summer";
            if (month >= 9 && month <= 11) return "Fall";
            return "Winter";
        }

        public decimal CalculatePriceWithTax(decimal price, decimal taxRate = 0.10m)
        {
            return price * (1 + taxRate);
        }

        public decimal CalculateRefundAmount(decimal paidAmount, DateTime checkInDate, DateTime cancellationDate)
        {
            var daysUntilCheckIn = (checkInDate - cancellationDate).Days;

            if (daysUntilCheckIn >= 7)
                return paidAmount; // Full refund
            else if (daysUntilCheckIn >= 3)
                return paidAmount * 0.50m; // 50% refund
            else if (daysUntilCheckIn >= 1)
                return paidAmount * 0.25m; // 25% refund
            else
                return 0; // No refund
        }
    }
}