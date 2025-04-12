using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Project.Migrations
{
    /// <inheritdoc />
    public partial class FixUserRoleAssignmentRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAssignments_UserRoles_UserRoleRoleID",
                table: "UserRoleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAssignments_Users_UserID1",
                table: "UserRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserRoleAssignments_UserID1",
                table: "UserRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserRoleAssignments_UserRoleRoleID",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "UserID1",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "UserRoleRoleID",
                table: "UserRoleAssignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserID1",
                table: "UserRoleAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserRoleRoleID",
                table: "UserRoleAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserID1",
                table: "UserRoleAssignments",
                column: "UserID1");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserRoleRoleID",
                table: "UserRoleAssignments",
                column: "UserRoleRoleID");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAssignments_UserRoles_UserRoleRoleID",
                table: "UserRoleAssignments",
                column: "UserRoleRoleID",
                principalTable: "UserRoles",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAssignments_Users_UserID1",
                table: "UserRoleAssignments",
                column: "UserID1",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
