using DevPulse.Infrastructure.Episodes;
using DevPulse.Infrastructure.TopicQueue;
using Microsoft.EntityFrameworkCore;

namespace DevPulse.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<EpisodeEntity>     Episodes   => Set<EpisodeEntity>();
    public DbSet<TopicQueueEntity>  TopicQueue => Set<TopicQueueEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<TopicQueueEntity>(t =>
        {
            t.ToTable("topic_queue");
            t.HasKey(x => x.Id);
            t.Property(x => x.Id).HasColumnName("id");
            t.Property(x => x.Concept).HasColumnName("concept");
            t.Property(x => x.Tag).HasColumnName("tag");
            t.Property(x => x.Language).HasColumnName("language");
            t.Property(x => x.Runnable).HasColumnName("runnable");
            t.Property(x => x.ForeshadowNext).HasColumnName("foreshadow_next");
            t.Property(x => x.Priority).HasColumnName("priority");
            t.Property(x => x.Status).HasColumnName("status");
            t.Property(x => x.Source).HasColumnName("source");
            t.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

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
