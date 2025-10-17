using System.ComponentModel.DataAnnotations;

namespace StrideLedger.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        // SHA256 hash of the refresh token string
        [Required]
        public string TokenHash { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public DateTime Expires { get; set; }

        [Required]
        public DateTime Created { get; set; }

        public string? CreatedByIp { get; set; }

        public DateTime? Revoked { get; set; }

        public string? RevokedByIp { get; set; }

        public string? ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;

        public bool IsActive => Revoked == null && !IsExpired;
    }
}