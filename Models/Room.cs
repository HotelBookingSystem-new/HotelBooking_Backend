public class Room
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string RoomNumber { get; set; }
    public int RoomCategoryId { get; set; }
    public int FloorNumber { get; set; }
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } // Available, Occupied, Maintenance, Reserved
    public Hotel Hotel { get; set; }
    public RoomCategory RoomCategory { get; set; }
    public ICollection<Booking> Bookings { get; set; }
}