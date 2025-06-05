using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Project.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleNamesConsistency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Blacklists",
                table: "Blacklists");

            migrationBuilder.RenameTable(
                name: "Blacklists",
                newName: "Blacklist");

            migrationBuilder.AddColumn<int>(
                name: "AvailableQuantity",
                table: "Equipment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Blacklist",
                table: "Blacklist",
                column: "BlacklistID");

            // Update the role name from "WarehouseOperative" to "WarehouseOperator" to match controllers and UI
            migrationBuilder.Sql("UPDATE UserRoles SET RoleName = 'WarehouseOperator' WHERE RoleName = 'WarehouseOperative'");
            
            // Also update any existing user records that might have the old role name
            migrationBuilder.Sql("UPDATE Users SET Role = 'WarehouseOperator' WHERE Role = 'WarehouseOperative'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Blacklist",
                table: "Blacklist");

            migrationBuilder.DropColumn(
                name: "AvailableQuantity",
                table: "Equipment");

            migrationBuilder.RenameTable(
                name: "Blacklist",
                newName: "Blacklists");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Blacklists",
                table: "Blacklists",
                column: "BlacklistID");

            // Revert the changes
            migrationBuilder.Sql("UPDATE UserRoles SET RoleName = 'WarehouseOperative' WHERE RoleName = 'WarehouseOperator'");
            migrationBuilder.Sql("UPDATE Users SET Role = 'WarehouseOperative' WHERE Role = 'WarehouseOperator'");
        }
    }
}
