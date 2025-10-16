using SharedKernel.Entities;
using SharedKernel.Constants;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuthService.Domain.Entities
{
    public class User : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public override string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("email")]
        public required string Email { get; set; }

        [BsonElement("passwordHash")]
        public required string PasswordHash { get; set; }

        [BsonElement("refreshToken")]
        public string? RefreshToken { get; set; }

        [BsonElement("role")]
        [BsonRepresentation(BsonType.String)]
        public UserRole Role { get; set; } = UserRole.User;

        [BsonElement("refreshTokenExpiryTime")]
        public DateTime? RefreshTokenExpiryTime { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        // Profile fields
        [BsonElement("fullName")]
        public string? FullName { get; set; }

        [BsonElement("displayName")]
        public string? DisplayName { get; set; }

        [BsonElement("phone")]
        public string? Phone { get; set; }

        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [BsonElement("bio")]
        public string? Bio { get; set; }

        [BsonElement("address")]
        public string? Address { get; set; }

        // UpdatedAt is inherited from BaseEntity - no duplicate field here
    }
}
