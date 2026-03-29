using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeruShopHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "marketplace_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalItemId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuyerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AnswerText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuestionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnswerDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_questions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_questions_TenantId_ExternalId",
                table: "marketplace_questions",
                columns: new[] { "TenantId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_questions_TenantId_ExternalItemId",
                table: "marketplace_questions",
                columns: new[] { "TenantId", "ExternalItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_questions_TenantId_Status",
                table: "marketplace_questions",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marketplace_questions");
        }
    }
}
