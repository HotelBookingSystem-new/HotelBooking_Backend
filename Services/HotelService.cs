using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Data;

namespace Backend.Services
{
    public class HotelService
    {
        private readonly ApplicationDbContext _context;

        // Booking statuses that block availability
        private static readonly HashSet<string> BlockingStatuses = new()
        {
            "Pending", "Confirmed", "CheckedIn", "CheckedOut"
        };

        public HotelService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------- Hotel CRUD ----------
        public async Task<IEnumerable<HotelDto>> GetAllHotelsAsync()
        {
            var hotels = await _context.Hotels
                .Include(h => h.Rooms).ThenInclude(r => r.RoomCategory)
                .Include(h => h.Amenities)
                .Where(h => h.IsActive)
                .ToListAsync();
            return hotels.Select(MapToHotelDto);
        }

        public async Task<HotelDto?> GetHotelByIdAsync(int id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Rooms).ThenInclude(r => r.RoomCategory)
                .Include(h => h.Amenities)
                .FirstOrDefaultAsync(h => h.Id == id && h.IsActive);
            return hotel == null ? null : MapToHotelDto(hotel);
        }

        public async Task<Hotel> CreateHotelAsync(CreateHotelDto dto)
        {
            var hotel = new Hotel
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Country = dto.Country,
                ZipCode = dto.ZipCode,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                Website = dto.Website,
                StarRating = dto.StarRating,
                CheckInTime = dto.CheckInTime,
                CheckOutTime = dto.CheckOutTime,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Hotels.Add(hotel);
            await _context.SaveChangesAsync();

            if (dto.AmenityIds != null && dto.AmenityIds.Any())
            {
                var amenities = await _context.Amenities
                    .Where(a => dto.AmenityIds.Contains(a.Id))
                    .ToListAsync();
                hotel.Amenities = amenities;
                await _context.SaveChangesAsync();
            }
            return hotel;
        }

        public async Task<bool> UpdateHotelAsync(int id, UpdateHotelDto dto)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null) return false;

            if (!string.IsNullOrEmpty(dto.Name)) hotel.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Description)) hotel.Description = dto.Description;
            if (!string.IsNullOrEmpty(dto.Address)) hotel.Address = dto.Address;
            if (!string.IsNullOrEmpty(dto.City)) hotel.City = dto.City;
            if (!string.IsNullOrEmpty(dto.State)) hotel.State = dto.State;
            if (!string.IsNullOrEmpty(dto.Country)) hotel.Country = dto.Country;
            if (!string.IsNullOrEmpty(dto.ZipCode)) hotel.ZipCode = dto.ZipCode;
            if (dto.Latitude.HasValue) hotel.Latitude = dto.Latitude.Value;
            if (dto.Longitude.HasValue) hotel.Longitude = dto.Longitude.Value;
            if (!string.IsNullOrEmpty(dto.PhoneNumber)) hotel.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrEmpty(dto.Email)) hotel.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Website)) hotel.Website = dto.Website;
            if (dto.StarRating.HasValue) hotel.StarRating = dto.StarRating.Value;
            if (!string.IsNullOrEmpty(dto.CheckInTime)) hotel.CheckInTime = dto.CheckInTime;
            if (!string.IsNullOrEmpty(dto.CheckOutTime)) hotel.CheckOutTime = dto.CheckOutTime;
            if (dto.IsActive.HasValue) hotel.IsActive = dto.IsActive.Value;

            hotel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteHotelAsync(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null) return false;
            _context.Hotels.Remove(hotel);
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Room CRUD ----------
        public async Task<Room?> AddRoomAsync(CreateRoomDto dto)
        {
            var hotel = await _context.Hotels.FindAsync(dto.HotelId);
            if (hotel == null || !hotel.IsActive) return null;

            var category = await _context.RoomCategories.FindAsync(dto.RoomCategoryId);
            if (category == null) return null;

            var room = new Room
            {
                HotelId = dto.HotelId,
                RoomNumber = dto.RoomNumber,
                RoomCategoryId = dto.RoomCategoryId,
                FloorNumber = dto.FloorNumber,
                Capacity = dto.Capacity,
                BasePrice = dto.BasePrice,
                IsAvailable = true,
                IsActive = true,
                Status = "Available"
            };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task<bool> DeleteRoomAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return false;
            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Amenity CRUD ----------
        public async Task<IEnumerable<AmenityDto>> GetAllAmenitiesAsync()
        {
            var amenities = await _context.Amenities.ToListAsync();
            return amenities.Select(a => new AmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                IsFree = a.IsFree,
                AdditionalCost = a.AdditionalCost
            });
        }

        public async Task<Amenity> CreateAmenityAsync(CreateAmenityDto dto)
        {
            var amenity = new Amenity
            {
                Name = dto.Name,
                Description = dto.Description,
                IsFree = dto.IsFree,
                AdditionalCost = dto.AdditionalCost
            };
            _context.Amenities.Add(amenity);
            await _context.SaveChangesAsync();
            return amenity;
        }

        // ---------- Availability Methods (called by Member C) ----------
        public async Task<bool> IsRoomCategoryAvailableAsync(int hotelId, int categoryId, DateTime checkIn, DateTime checkOut)
        {
            var anyRoomAvailable = await _context.Rooms
                .Where(r => r.HotelId == hotelId && r.RoomCategoryId == categoryId && r.IsActive && r.IsAvailable)
                .AnyAsync(r => !_context.Bookings.Any(b =>
                    b.RoomId == r.Id &&
                    BlockingStatuses.Contains(b.Status) &&
                    DateValidator.Overlaps(b.CheckInDate, b.CheckOutDate, checkIn, checkOut)));
            return anyRoomAvailable;
        }

        public async Task<bool> BlockAvailabilityAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            var isAvailable = !await _context.Bookings.AnyAsync(b =>
                b.RoomId == roomId &&
                BlockingStatuses.Contains(b.Status) &&
                DateValidator.Overlaps(b.CheckInDate, b.CheckOutDate, checkIn, checkOut));
            return isAvailable;
        }

        // ---------- Search & Filter ----------
        public async Task<SearchResultDto> SearchHotelsAsync(SearchFilterDto filter)
        {
            var query = _context.Hotels
                .Include(h => h.Rooms).ThenInclude(r => r.RoomCategory)
                .Include(h => h.Amenities)
                .Where(h => h.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.City))
                query = query.Where(h => h.City.Contains(filter.City));
            if (!string.IsNullOrEmpty(filter.Country))
                query = query.Where(h => h.Country.Contains(filter.Country));
            if (filter.MinStarRating.HasValue)
                query = query.Where(h => h.StarRating >= filter.MinStarRating.Value);
            if (filter.MaxStarRating.HasValue)
                query = query.Where(h => h.StarRating <= filter.MaxStarRating.Value);
            if (filter.Amenities != null && filter.Amenities.Any())
            {
                var amenityNames = filter.Amenities.Select(a => a.ToLower()).ToList();
                query = query.Where(h => h.Amenities.Any(a => amenityNames.Contains(a.Name.ToLower())));
            }
            if (!string.IsNullOrEmpty(filter.RoomCategory))
            {
                var catName = filter.RoomCategory.ToLower();
                query = query.Where(h => h.Rooms.Any(r => r.RoomCategory.Name.ToLower() == catName));
            }

            if (filter.CheckInDate.HasValue && filter.CheckOutDate.HasValue)
            {
                var checkIn = filter.CheckInDate.Value;
                var checkOut = filter.CheckOutDate.Value;
                if (!DateValidator.IsValidDateRange(checkIn, checkOut))
                    throw new ArgumentException("Invalid date range");

                var totalNights = DateValidator.GetTotalNights(checkIn, checkOut);
                var guests = filter.NumberOfGuests ?? 1;

                query = query.Where(h => h.Rooms.Any(r =>
                    r.IsActive && r.IsAvailable && r.Capacity >= guests &&
                    !_context.Bookings.Any(b =>
                        b.RoomId == r.Id &&
                        BlockingStatuses.Contains(b.Status) &&
                        DateValidator.Overlaps(b.CheckInDate, b.CheckOutDate, checkIn, checkOut))));

                var hotelsList = await query.ToListAsync();
                var results = new List<HotelSearchResultDto>();

                foreach (var hotel in hotelsList)
                {
                    var availableRooms = hotel.Rooms.Where(r =>
                        r.IsActive && r.IsAvailable && r.Capacity >= guests &&
                        !_context.Bookings.Any(b =>
                            b.RoomId == r.Id &&
                            BlockingStatuses.Contains(b.Status) &&
                            DateValidator.Overlaps(b.CheckInDate, b.CheckOutDate, checkIn, checkOut))).ToList();

                    if (!availableRooms.Any()) continue;

                    var lowestPricePerNight = availableRooms.Min(r => r.BasePrice);
                    var totalPrice = lowestPricePerNight * totalNights;

                    if (filter.MinPrice.HasValue && totalPrice < filter.MinPrice) continue;
                    if (filter.MaxPrice.HasValue && totalPrice > filter.MaxPrice) continue;

                    results.Add(new HotelSearchResultDto
                    {
                        Id = hotel.Id,
                        Name = hotel.Name,
                        City = hotel.City,
                        Country = hotel.Country,
                        StarRating = hotel.StarRating,
                        Description = hotel.Description,
                        LowestPricePerNight = lowestPricePerNight,
                        AvailableRooms = availableRooms.Count,
                        TopAmenities = hotel.Amenities.Take(5).Select(a => a.Name).ToList(),
                        DistanceFromCenter = 0,
                        AverageRating = hotel.StarRating,
                        TotalReviews = 0
                    });
                }

                results = filter.SortBy?.ToLower() switch
                {
                    "priceasc" => results.OrderBy(r => r.LowestPricePerNight).ToList(),
                    "pricedesc" => results.OrderByDescending(r => r.LowestPricePerNight).ToList(),
                    "ratingdesc" => results.OrderByDescending(r => r.StarRating).ToList(),
                    "nameasc" => results.OrderBy(r => r.Name).ToList(),
                    _ => results.OrderBy(r => r.Name).ToList()
                };

                var total = results.Count;
                var paged = results.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToList();
                return new SearchResultDto
                {
                    Hotels = paged,
                    TotalCount = total,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                    HasNextPage = filter.PageNumber * filter.PageSize < total,
                    HasPreviousPage = filter.PageNumber > 1
                };
            }

            // No dates – simple search
            var simpleHotels = await query.ToListAsync();
            var simpleResults = simpleHotels.Select(h => new HotelSearchResultDto
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City,
                Country = h.Country,
                StarRating = h.StarRating,
                Description = h.Description,
                LowestPricePerNight = h.Rooms.Any() ? h.Rooms.Min(r => r.BasePrice) : 0,
                AvailableRooms = h.Rooms.Count(r => r.IsActive && r.IsAvailable),
                TopAmenities = h.Amenities.Take(5).Select(a => a.Name).ToList(),
                AverageRating = h.StarRating
            }).ToList();

            simpleResults = filter.SortBy?.ToLower() switch
            {
                "priceasc" => simpleResults.OrderBy(r => r.LowestPricePerNight).ToList(),
                "pricedesc" => simpleResults.OrderByDescending(r => r.LowestPricePerNight).ToList(),
                "ratingdesc" => simpleResults.OrderByDescending(r => r.StarRating).ToList(),
                "nameasc" => simpleResults.OrderBy(r => r.Name).ToList(),
                _ => simpleResults.OrderBy(r => r.Name).ToList()
            };

            var totalSimple = simpleResults.Count;
            var pagedSimple = simpleResults.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToList();
            return new SearchResultDto
            {
                Hotels = pagedSimple,
                TotalCount = totalSimple,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalSimple / (double)filter.PageSize),
                HasNextPage = filter.PageNumber * filter.PageSize < totalSimple,
                HasPreviousPage = filter.PageNumber > 1
            };
        }

        // ---------- Private Mapper ----------
        private HotelDto MapToHotelDto(Hotel hotel)
        {
            return new HotelDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                Description = hotel.Description,
                Address = hotel.Address,
                City = hotel.City,
                State = hotel.State,
                Country = hotel.Country,
                ZipCode = hotel.ZipCode,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                PhoneNumber = hotel.PhoneNumber,
                Email = hotel.Email,
                Website = hotel.Website,
                StarRating = hotel.StarRating,
                CheckInTime = hotel.CheckInTime,
                CheckOutTime = hotel.CheckOutTime,
                Rooms = hotel.Rooms.Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    FloorNumber = r.FloorNumber,
                    Capacity = r.Capacity,
                    BasePrice = r.BasePrice,
                    IsAvailable = r.IsAvailable,
                    Status = r.Status,
                    RoomCategory = r.RoomCategory == null ? null : new RoomCategoryDto
                    {
                        Id = r.RoomCategory.Id,
                        Name = r.RoomCategory.Name,
                        Description = r.RoomCategory.Description,
                        PriceMultiplier = r.RoomCategory.PriceMultiplier,
                        MaxOccupancy = r.RoomCategory.MaxOccupancy,
                        BedType = r.RoomCategory.BedType,
                        BedCount = r.RoomCategory.BedCount,
                        SquareFootage = r.RoomCategory.SquareFootage,
                        View = r.RoomCategory.View
                    }
                }).ToList(),
                Amenities = hotel.Amenities.Select(a => new AmenityDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    IsFree = a.IsFree,
                    AdditionalCost = a.AdditionalCost
                }).ToList()
            };
        }
    }
}