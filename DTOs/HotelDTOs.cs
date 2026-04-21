using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class HotelDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public int StarRating { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
        public List<RoomDto> Rooms { get; set; }
        public List<AmenityDto> Amenities { get; set; }
    }

    public class CreateHotelDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; }

        [MaxLength(100)]
        public string State { get; set; }

        [Required]
        [MaxLength(100)]
        public string Country { get; set; }

        [Required]
        [MaxLength(20)]
        public string ZipCode { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Url]
        public string Website { get; set; }

        [Range(1, 5)]
        public int StarRating { get; set; }

        [Required]
        public string CheckInTime { get; set; }

        [Required]
        public string CheckOutTime { get; set; }

        public List<int> AmenityIds { get; set; }
    }

    public class UpdateHotelDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public int? StarRating { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
        public bool? IsActive { get; set; }
    }

    public class RoomDto
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int FloorNumber { get; set; }
        public int Capacity { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsAvailable { get; set; }
        public string Status { get; set; }
        public RoomCategoryDto RoomCategory { get; set; }
    }

    public class RoomCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PriceMultiplier { get; set; }
        public int MaxOccupancy { get; set; }
        public string BedType { get; set; }
        public int BedCount { get; set; }
        public int SquareFootage { get; set; }
        public string View { get; set; }
    }

    public class CreateRoomDto
    {
        [Required]
        public int HotelId { get; set; }

        [Required]
        public string RoomNumber { get; set; }

        [Required]
        public int RoomCategoryId { get; set; }

        public int FloorNumber { get; set; }

        [Range(1, 10)]
        public int Capacity { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal BasePrice { get; set; }
    }

    public class AmenityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsFree { get; set; }
        public decimal AdditionalCost { get; set; }
    }

    public class CreateAmenityDto
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsFree { get; set; }

        public decimal AdditionalCost { get; set; }
    }

    public class HotelSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public int StarRating { get; set; }
        public decimal LowestPrice { get; set; }
        public int AvailableRooms { get; set; }
        public List<string> TopAmenities { get; set; }
    }
}