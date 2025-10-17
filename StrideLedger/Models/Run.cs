using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrideLedger.Models
{
    public class Run
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RunId { get; set; }

        [ForeignKey(nameof(Shoe))]
        public int ShoeId { get; set; }

        public Shoe? Shoe { get; set; }
        public DateTime Date { get; set; }
        public double DistanceKm { get; set; }
        public double DistanceMile { get; set; }
    }
}
