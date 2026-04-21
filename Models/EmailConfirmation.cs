public class EmailConfirmation
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string RecipientEmail { get; set; }
    public string ConfirmationNumber { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsDelivered { get; set; }
    public string EmailContent { get; set; }
    public DateTime? OpenedAt { get; set; }
}