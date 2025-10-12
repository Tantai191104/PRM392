namespace SharedKernel.Entities
{
    public abstract class BaseEntity
    {
        public virtual string Id { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
