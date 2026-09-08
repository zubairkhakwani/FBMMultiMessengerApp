using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIndexUpdatedInChatMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ChatMessages
WHERE Id NOT IN (
    SELECT MIN(Id)
    FROM ChatMessages
    GROUP BY FbMessageId, ChatId
);");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_FbMessageId_ChatId",
                table: "ChatMessages",
                columns: new[] { "FbMessageId", "ChatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_FbMessageId_ChatId",
                table: "ChatMessages");
        }
    }
}
