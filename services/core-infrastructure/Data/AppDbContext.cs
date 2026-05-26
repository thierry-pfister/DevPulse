using DevPulse.Infrastructure.Episodes;
using Microsoft.EntityFrameworkCore;

namespace DevPulse.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<EpisodeEntity> Episodes => Set<EpisodeEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<EpisodeEntity>(e =>
        {
            e.ToTable("episodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Concept).HasColumnName("concept");
            e.Property(x => x.Tag).HasColumnName("tag");
            e.Property(x => x.Language).HasColumnName("language");
            e.Property(x => x.EpisodeNumber).HasColumnName("episode_number");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.ContentJson).HasColumnName("content_json");
            e.Property(x => x.WasEdited).HasColumnName("was_edited");
            e.Property(x => x.PublishAt).HasColumnName("publish_at");
            e.Property(x => x.PublishedAt).HasColumnName("published_at");
            e.Property(x => x.PlatformIdsJson).HasColumnName("platform_ids");
            e.Property(x => x.PreviousEpisodeId).HasColumnName("previous_episode_id");
            e.Property(x => x.GeneratedAt).HasColumnName("generated_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
