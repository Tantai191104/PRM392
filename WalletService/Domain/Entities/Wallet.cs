namespace WalletService.Domain.Entities
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public class Wallet
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public decimal Balance { get; set; }
    }
}