using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduChatbot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseIdToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "course_id",
                table: "chat_conversations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_course_id",
                table: "chat_conversations",
                column: "course_id");

            migrationBuilder.AddForeignKey(
                name: "FK_chat_conversations_courses_course_id",
                table: "chat_conversations",
                column: "course_id",
                principalTable: "courses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chat_conversations_courses_course_id",
                table: "chat_conversations");

            migrationBuilder.DropIndex(
                name: "IX_chat_conversations_course_id",
                table: "chat_conversations");

            migrationBuilder.DropColumn(
                name: "course_id",
                table: "chat_conversations");
        }
    }
}
