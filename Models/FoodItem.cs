namespace HotelPOS.Wpf.Models
{
    public sealed class FoodItem
    {
        public int FoodID { get; set; }
        public string FoodName { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
    }
}
