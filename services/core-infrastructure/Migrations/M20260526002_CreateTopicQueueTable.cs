using FluentMigrator;

namespace DevPulse.Infrastructure.Migrations;

[Migration(20260526002)]
public class M20260526002_CreateTopicQueueTable : Migration
{
    public override void Up() =>
        Create.Table("topic_queue")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("concept").AsCustom("text").NotNullable()
            .WithColumn("tag").AsString(100).NotNullable()
            .WithColumn("language").AsString(100).Nullable()
            .WithColumn("runnable").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("foreshadow_next").AsString(255).Nullable()
            .WithColumn("priority").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("pending")
            .WithColumn("source").AsString(50).NotNullable().WithDefaultValue("manual")
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

    public override void Down() => Delete.Table("topic_queue");
}
