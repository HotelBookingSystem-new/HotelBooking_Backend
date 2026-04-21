using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HotelsController : ControllerBase
    {
        private readonly HotelService _hotelService;

        public HotelsController(HotelService hotelService)
        {
            _hotelService = hotelService;
        }

        // Public endpoints
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var hotels = await _hotelService.GetAllHotelsAsync();
            return Ok(hotels);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var hotel = await _hotelService.GetHotelByIdAsync(id);
            if (hotel == null) return NotFound();
            return Ok(hotel);
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchFilterDto filter)
        {
            try
            {
                var result = await _hotelService.SearchHotelsAsync(filter);
                return Ok(result);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("amenities")]
        public async Task<IActionResult> GetAllAmenities()
        {
            var amenities = await _hotelService.GetAllAmenitiesAsync();
            return Ok(amenities);
        }

        // Admin endpoints
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateHotel([FromBody] CreateHotelDto dto)
        {
            var hotel = await _hotelService.CreateHotelAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, hotel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHotel(int id, [FromBody] UpdateHotelDto dto)
        {
            var success = await _hotelService.UpdateHotelAsync(id, dto);
            if (!success) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            var success = await _hotelService.DeleteHotelAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("rooms")]
        public async Task<IActionResult> AddRoom([FromBody] CreateRoomDto dto)
        {
            var room = await _hotelService.AddRoomAsync(dto);
            if (room == null) return BadRequest("Invalid hotel or room category");
            return Ok(room);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("rooms/{roomId}")]
        public async Task<IActionResult> DeleteRoom(int roomId)
        {
            var success = await _hotelService.DeleteRoomAsync(roomId);
            if (!success) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("amenities")]
        public async Task<IActionResult> CreateAmenity([FromBody] CreateAmenityDto dto)
        {
            var amenity = await _hotelService.CreateAmenityAsync(dto);
            return Ok(amenity);
        }
    }
}