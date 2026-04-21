public class Amenity
{
    public int Id { get; set; }
    public string Name { get; set; } // WiFi, Pool, Gym, Restaurant, Parking, Spa
    public string Description { get; set; }
    public bool IsFree { get; set; }
    public decimal AdditionalCost { get; set; }
    public ICollection<Hotel> Hotels { get; set; }
}