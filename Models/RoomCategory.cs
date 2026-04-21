public class RoomCategory
{
    public int Id { get; set; }
    public string Name { get; set; } // Standard, Deluxe, Suite, Presidential
    public string Description { get; set; }
    public decimal PriceMultiplier { get; set; }
    public int MaxOccupancy { get; set; }
    public string BedType { get; set; } // Single, Double, Queen, King
    public int BedCount { get; set; }
    public int SquareFootage { get; set; }
    public string View { get; set; } // City, Ocean, Mountain, Garden
    public ICollection<Room> Rooms { get; set; }
}