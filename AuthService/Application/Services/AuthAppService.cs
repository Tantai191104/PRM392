using AuthService.Domain.Entities;
using AuthService.Infrastructure.Repositories;
using AuthService.Application.DTOs;
using BCrypt.Net;

namespace AuthService.Application.Services
{
    public class AuthAppService
    {
        private readonly UserRepository _userRepo;
        private readonly JwtService _jwtService;

        public AuthAppService(UserRepository userRepo, JwtService jwtService)
        {
            _userRepo = userRepo;
            _jwtService = jwtService;
        }

        public async Task<RegisterResultDto> Register(RegisterDto dto)
        {
            // Basic validations (use dto)
            var email = dto.Email;
            var password = dto.Password;
            if (string.IsNullOrWhiteSpace(email))
                return new RegisterResultDto { Success = false, Error = "Email is required" };

            var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailValidator.IsValid(email))
                return new RegisterResultDto { Success = false, Error = "Email is not valid" };

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return new RegisterResultDto { Success = false, Error = "Password must be at least 8 characters" };

            var hasLetter = password.Any(char.IsLetter);
            var hasDigit = password.Any(char.IsDigit);
            if (!hasLetter || !hasDigit)
                return new RegisterResultDto { Success = false, Error = "Password must contain letters and numbers" };

            var existing = await _userRepo.GetByEmailAsync(email);
            if (existing != null)
                return new RegisterResultDto { Success = false, Error = "Email already in use" };

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = dto.FullName,
                Phone = dto.Phone,
                Address = dto.Address,
                AvatarUrl = "https://ui-avatars.com/api/?name=User"
            };

            await _userRepo.CreateAsync(user);

            var userDto = new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                DisplayName = user.DisplayName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Address = user.Address,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            };

            return new RegisterResultDto { Success = true, User = userDto };
        }

        public async Task<LoginResultDto?> Login(string email, string password)
        {

            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;
            // Block login if user is banned (IsActive == false)
            if (!user.IsActive)
                return new LoginResultDto { Tokens = null, User = null, Error = "Tài khoản đã bị khóa/banned" };

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepo.UpdateAsync(user);

            var tokenDto = new TokenResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };

            var userDto = new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                DisplayName = user.DisplayName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Address = user.Address,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            };

            return new LoginResultDto { Tokens = tokenDto, User = userDto };
        }

        public async Task<TokenResponseDto?> RefreshToken(string refreshToken)
        {
            var user = await _userRepo.GetByRefreshTokenAsync(refreshToken);
            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow) return null;

            var accessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepo.UpdateAsync(user);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task Logout(string refreshToken)
        {
            var user = await _userRepo.GetByRefreshTokenAsync(refreshToken);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _userRepo.UpdateAsync(user);
            }
        }
    }
}
