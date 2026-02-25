using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIndexInChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Chats_FBChatId_UserId",
                table: "Chats",
                columns: new[] { "FBChatId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chats_FBChatId_UserId",
                table: "Chats");
        }
    }
}
