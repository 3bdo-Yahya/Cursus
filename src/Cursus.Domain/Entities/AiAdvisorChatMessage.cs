namespace Cursus.Domain.Entities
{
    public class AiAdvisorChatMessage
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public AppUser? Student { get; set; }
    }
}
