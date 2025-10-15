using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace StrideLedger.Models
{
    public class Shoe : IValidatableObject
    {
        public int ShoeId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Brand is required.")]
        [StringLength(50, ErrorMessage = "Brand cannot exceed 50 characters.")]
        public string Brand { get; set; } = null!;

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, ErrorMessage = "Model cannot exceed 50 characters.")]
        public string Model { get; set; } = null!;

        [Range(0.1, 10000, ErrorMessage = "TargetMileage must be greater than 0.")]
        public double TargetMileage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "CurrentMileage cannot be negative.")]
        public double CurrentMileage { get; set; } = 0;

        // Computed property for RemainingMileage
        public double RemainingMileage
        {
            get
            {
                var remaining = TargetMileage - CurrentMileage;
                return remaining < 0 ? 0 : remaining;
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Regex to detect script tags or suspicious input
            var scriptPattern = @"<\s*script\b|on\w+\s*=";
            if (!string.IsNullOrEmpty(Name) && Regex.IsMatch(Name, scriptPattern, RegexOptions.IgnoreCase))
            {
                yield return new ValidationResult("Name contains invalid or potentially dangerous content.", new[] { nameof(Name) });
            }
            if (!string.IsNullOrEmpty(Description) && Regex.IsMatch(Description, scriptPattern, RegexOptions.IgnoreCase))
            {
                yield return new ValidationResult("Description contains invalid or potentially dangerous content.", new[] { nameof(Description) });
            }
            if (!string.IsNullOrEmpty(Brand) && Regex.IsMatch(Brand, scriptPattern, RegexOptions.IgnoreCase))
            {
                yield return new ValidationResult("Brand contains invalid or potentially dangerous content.", new[] { nameof(Brand) });
            }
            if (!string.IsNullOrEmpty(Model) && Regex.IsMatch(Model, scriptPattern, RegexOptions.IgnoreCase))
            {
                yield return new ValidationResult("Model contains invalid or potentially dangerous content.", new[] { nameof(Model) });
            }

            if (CurrentMileage > TargetMileage)
            {
                yield return new ValidationResult(
                    "CurrentMileage cannot exceed TargetMileage.",
                    new[] { nameof(CurrentMileage) });
            }
        }
    }
}
