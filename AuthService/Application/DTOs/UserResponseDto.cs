namespace AuthService.Application.DTOs
{
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? DisplayName { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Address { get; set; }
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; }
    }
}