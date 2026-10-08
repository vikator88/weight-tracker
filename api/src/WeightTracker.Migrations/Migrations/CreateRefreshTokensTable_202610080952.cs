using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080952)]
public sealed class CreateRefreshTokensTable_202610080952 : Migration
{
    public override void Up()
    {
        Create.Table("refresh_tokens")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_refresh_tokens")
            .WithColumn("user_id").AsGuid().NotNullable()
            // Only the digest is stored; the raw token never reaches the database.
            // Length aligned with RefreshToken.TokenHashMaxLength
            .WithColumn("token_hash").AsString(128).NotNullable()
            .WithColumn("expires_at").AsCustom("timestamptz").NotNullable()
            .WithColumn("revoked_at").AsCustom("timestamptz").Nullable()
            .WithColumn("replaced_by_token_id").AsGuid().Nullable()
            .WithColumn("created_at").AsCustom("timestamptz").NotNullable();

        Create.ForeignKey("fk_refresh_tokens_user")
            .FromTable("refresh_tokens").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("fk_refresh_tokens_replaced_by")
            .FromTable("refresh_tokens").ForeignColumn("replaced_by_token_id")
            .ToTable("refresh_tokens").PrimaryColumn("id");

        // Lookup on redemption happens by hash
        Create.UniqueConstraint("uq_refresh_tokens_token_hash")
            .OnTable("refresh_tokens").Column("token_hash");

        Create.Index("ix_refresh_tokens_user_id")
            .OnTable("refresh_tokens").OnColumn("user_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("refresh_tokens");
    }
}
