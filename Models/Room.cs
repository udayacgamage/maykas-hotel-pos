namespace HotelPOS.Wpf.Models
{
    public sealed class Room
    {
        public int RoomID { get; set; }
        public string RoomNumber { get; set; } = "";
        public string RoomType { get; set; } = "";
        public decimal PricePerDay { get; set; }
        public string Status { get; set; } = "";
    }
}
