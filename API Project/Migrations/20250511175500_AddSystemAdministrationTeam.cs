using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemAdministrationTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert the System Administration Team with ID 0
            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "TeamID", "TeamName", "Description", "IsActive", "CreatedDate", "IsBlacklisted" },
                values: new object[] { 0, "System Administration", "System team for equipment operations", true, new DateTime(2025, 5, 11), false }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the System Administration Team
            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "TeamID",
                keyValue: 0);
        }
    }
}
