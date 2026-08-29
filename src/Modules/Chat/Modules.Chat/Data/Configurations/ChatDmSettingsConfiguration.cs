using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class ChatDmSettingsConfiguration : IEntityTypeConfiguration<ChatDmSettings>
{
    public void Configure(EntityTypeBuilder<ChatDmSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("DmSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AllowStudentToStudentDm).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
