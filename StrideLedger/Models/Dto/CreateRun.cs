// Plan (pseudocode & detailed steps to build):
// 1. Create a DTO named CreateRun in the Dtos folder/namespace so POST request bodies don't include RunId.
// 2. Include properties required for creating a Run:
//    - ShoeId (required, foreign key reference to existing Shoe)
//    - Date (required)
//    - DistanceKm (required, must be > 0)
//    - DistanceMile (optional; server can compute from DistanceKm if omitted)
// 3. Apply DataAnnotations to validate incoming POST payloads (Required, Range).
// 4. Keep DTO minimal: do not include RunId or navigation properties like Shoe to ensure OpenAPI request schema omits the ID.
// 5. Use namespace StrideLedger.Models.Dtos and target file path StrideLedger\Models\Dtos\CreateRun.cs.
// 6. This DTO will be used by controller POST endpoints; map it to the Run entity server-side (e.g., via manual mapping or AutoMapper).

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrideLedger.Models.Dtos
{
    public class CreateRun
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ReadOnly(true)]
        [Required(ErrorMessage = "ShoeId is required.")]
        public int ShoeId { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "DistanceKm is required.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "DistanceKm must be greater than 0.")]
        public double DistanceKm { get; set; }

        // Optional: server can compute this from DistanceKm if not supplied.
        [Range(0.0001, double.MaxValue, ErrorMessage = "DistanceMile must be greater than 0.")]
        public double? DistanceMile { get; set; }
    }
}
