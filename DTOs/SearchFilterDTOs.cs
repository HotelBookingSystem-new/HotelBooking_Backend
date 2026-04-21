using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.DTOs
{
    public class SearchFilterDto
    {
        public string City { get; set; }
        public string Country { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int? NumberOfGuests { get; set; }
        public int? MinStarRating { get; set; }
        public int? MaxStarRating { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<string> Amenities { get; set; }
        public string RoomCategory { get; set; }
        public string SortBy { get; set; } // PriceAsc, PriceDesc, RatingDesc, NameAsc
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SearchResultDto
    {
        public List<HotelSearchResultDto> Hotels { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class HotelSearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public int StarRating { get; set; }
        public string Description { get; set; }
        public decimal LowestPricePerNight { get; set; }
        public int AvailableRooms { get; set; }
        public List<string> TopAmenities { get; set; }
        public double DistanceFromCenter { get; set; } // in KM
        public string ImageUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }

    public class FilterOptionsDto
    {
        public List<string> Cities { get; set; }
        public List<int> StarRatings { get; set; }
        public List<string> Amenities { get; set; }
        public List<string> RoomCategories { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public List<string> Countries { get; set; }
    }

    public class DateRangeDto
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool ValidateDateRange()
        {
            return StartDate < EndDate && StartDate >= DateTime.Today;
        }

        public int GetNights()
        {
            return (EndDate - StartDate).Days;
        }
    }

    public class LocationFilterDto
    {
        public string City { get; set; }
        public string Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusInKm { get; set; }
    }

    public class PriceRangeDto
    {
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public bool IsValid()
        {
            if (MinPrice.HasValue && MaxPrice.HasValue)
                return MinPrice <= MaxPrice;
            return true;
        }
    }
}