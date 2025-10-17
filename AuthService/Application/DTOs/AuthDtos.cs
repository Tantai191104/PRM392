namespace AuthService.Application.DTOs
{
    public class RegisterDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public required string Email { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(8)]
        public required string Password { get; set; }
        
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string? FullName { get; set; }

    [System.ComponentModel.DataAnnotations.Phone]
    public string? Phone { get; set; }

    // Simple address string
    public string? Address { get; set; }

    // AvatarUrl không required
    [System.ComponentModel.DataAnnotations.Url]
    public string? AvatarUrl { get; set; }
    }

    public class LoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LoginResultDto
    {
        public TokenResponseDto Tokens { get; set; } = new TokenResponseDto();
        public UserResponseDto User { get; set; } = new UserResponseDto();
    }

    public class RegisterResultDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public UserResponseDto? User { get; set; }
    }
}
