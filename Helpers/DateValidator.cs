using System;

namespace Backend.Helpers
{
    public static class DateValidator
    {
        public static bool IsValidDateRange(DateTime checkIn, DateTime checkOut)
        {
            return checkIn >= DateTime.UtcNow.Date && checkOut > checkIn;
        }

        public static int GetTotalNights(DateTime checkIn, DateTime checkOut)
        {
            return (int)(checkOut - checkIn).TotalDays;
        }

        public static bool Overlaps(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1 < end2 && end1 > start2;
        }

        public static bool CanCancel(DateTime checkInDate, DateTime cancellationDeadline)
        {
            return DateTime.UtcNow < cancellationDeadline;
        }
    }
}