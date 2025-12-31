namespace HotelPOS.Wpf.Models
{
    public sealed class BillSummary
    {
        public long BillID { get; set; }
        public string BillDate { get; set; } = "";
        public string? RoomNumber { get; set; }
        public decimal TotalAmount { get; set; }

        public string BillNo => $"BILL-{BillID:D6}";
    }
}
