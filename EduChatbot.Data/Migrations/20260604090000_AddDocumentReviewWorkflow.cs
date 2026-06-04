using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduChatbot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "courses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "match_score",
                table: "documents",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_note",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewed_by_id",
                table: "documents",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_code",
                table: "documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "subject_name",
                table: "documents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE courses
                SET description = code || ' - ' || name
                WHERE description = '';
                """);

            migrationBuilder.Sql("""
                UPDATE documents AS d
                SET subject_code = c.code,
                    subject_name = c.name
                FROM courses AS c
                WHERE d.course_id = c.id
                  AND d.subject_code = ''
                  AND d.subject_name = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "match_score",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "review_note",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "reviewed_by_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "subject_code",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "subject_name",
                table: "documents");
        }
    }
}
