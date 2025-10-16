using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs
{
    public class UpdateProfileDto
    {
        [StringLength(200)]
        public string? FullName { get; set; }

        [StringLength(100)]
        public string? DisplayName { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [Url]
        public string? AvatarUrl { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }
        // Address (simple string)
        public string? Address { get; set; }
    }

}