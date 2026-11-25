using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryAndRecipientTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt_NextAttemptAt_OccurredAt",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadLetteredAt",
                table: "OutboxMessages",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessedMessageRecipients",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Consumer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipientHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedMessageRecipients", x => new { x.MessageId, x.Consumer, x.RecipientHash });
                    table.ForeignKey(
                        name: "FK_ProcessedMessageRecipients_ProcessedMessages_MessageId_Consu~",
                        columns: x => new { x.MessageId, x.Consumer },
                        principalTable: "ProcessedMessages",
                        principalColumns: new[] { "MessageId", "Consumer" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_DeadLetteredAt_NextAttemptAt_Occu~",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "DeadLetteredAt", "NextAttemptAt", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedMessageRecipients");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt_DeadLetteredAt_NextAttemptAt_Occu~",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_NextAttemptAt_OccurredAt",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "NextAttemptAt", "OccurredAt" });
        }
    }
}
