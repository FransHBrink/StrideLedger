namespace StrideLedger.Models
{
    public class Run
    {
        public int RunId { get; set; }
        public int ShoeId { get; set; }
        public Shoe? Shoe { get; set; }
        public DateTime Date { get; set; }
        public double DistanceKm { get; set; }
        public double DistanceMile { get; set; }
    }
}
