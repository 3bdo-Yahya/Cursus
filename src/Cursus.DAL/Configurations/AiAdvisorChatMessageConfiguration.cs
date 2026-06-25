using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class AiAdvisorChatMessageConfiguration : IEntityTypeConfiguration<AiAdvisorChatMessage>
    {
        public void Configure(EntityTypeBuilder<AiAdvisorChatMessage> builder)
        {
            builder.Property(message => message.StudentId)
                .IsRequired();

            builder.Property(message => message.Role)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(message => message.Content)
                .IsRequired()
                .HasMaxLength(8000);

            builder.Property(message => message.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(message => message.Student)
                .WithMany(student => student.AiAdvisorChatMessages)
                .HasForeignKey(message => message.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(message => new
            {
                message.StudentId,
                message.CreatedAtUtc
            });
        }
    }
}
