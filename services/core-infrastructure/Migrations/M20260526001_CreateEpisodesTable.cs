using FluentMigrator;

namespace DevPulse.Infrastructure.Migrations;

[Migration(20260526001)]
public class M20260526001_CreateEpisodesTable : Migration
{
    public override void Up()
    {
        Create.Table("episodes")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("concept").AsString(int.MaxValue).NotNullable()
            .WithColumn("tag").AsString(255).NotNullable()
            .WithColumn("language").AsString(255).Nullable()
            .WithColumn("episode_number").AsInt32().NotNullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("queued")
            .WithColumn("content_json").AsCustom("jsonb").Nullable()
            .WithColumn("was_edited").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("publish_at").AsDateTimeOffset().Nullable()
            .WithColumn("published_at").AsDateTimeOffset().Nullable()
            .WithColumn("platform_ids").AsCustom("jsonb").Nullable()
            .WithColumn("previous_episode_id").AsGuid().Nullable()
            .WithColumn("generated_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("episodes");
    }
}
