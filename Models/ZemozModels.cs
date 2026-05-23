namespace ZemozSmart.Models
{
    public class Supporter
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public List<Card> Cards { get; set; } = new();
    }

    public enum CardType
    {
        Aloba,
        Bossa,
        Amegan
    }

    public class Card
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public int SupporterId { get; set; }
        public Supporter? Supporter { get; set; }
        public CardType Type { get; set; }
        public int RemainingScans { get; set; } = 20;
        public bool IsBlocked { get; set; } = false;
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(20);
        public List<Scan> Scans { get; set; } = new();
    }

    public class Scan
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public Card? Card { get; set; }
        public DateTime ScanDate { get; set; } = DateTime.UtcNow;
    }

    public class Agent
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // In a real app, use BCrypt
    }
}
