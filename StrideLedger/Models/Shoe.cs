namespace StrideLedger.Models
{
    public class Shoe
    {
        public int ShoeId { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public double TargetMileage { get; set; }
        public double CurrentMileage { get; set; } = 0;
    }
}
