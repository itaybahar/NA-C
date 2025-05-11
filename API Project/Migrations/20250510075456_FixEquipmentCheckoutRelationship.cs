using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Project.Migrations
{
    /// <inheritdoc />
    public partial class FixEquipmentCheckoutRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentCheckouts_Equipment_EquipmentID",
                table: "EquipmentCheckouts");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentCheckouts_Equipment_EquipmentId",
                table: "EquipmentCheckouts");

            migrationBuilder.DropTable(
                name: "CheckoutRecord");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentCheckouts_EquipmentID",
                table: "EquipmentCheckouts");

            migrationBuilder.DropColumn(
                name: "CheckedOutBy",
                table: "EquipmentCheckouts");

            migrationBuilder.DropColumn(
                name: "ReturnApprovedBy",
                table: "EquipmentCheckouts");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                table: "EquipmentCheckouts",
                newName: "EquipmentID");

            migrationBuilder.RenameColumn(
                name: "EquipmentID",
                table: "EquipmentCheckouts",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_EquipmentCheckouts_EquipmentId",
                table: "EquipmentCheckouts",
                newName: "IX_EquipmentCheckouts_EquipmentID");

            migrationBuilder.AlterColumn<bool>(
                name: "IsBlacklisted",
                table: "Teams",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Teams",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Teams",
                type: "datetime(6)",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentID",
                table: "EquipmentCheckouts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "EquipmentCheckouts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCondition",
                table: "EquipmentCheckouts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnDate",
                table: "EquipmentCheckouts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StorageLocation",
                table: "Equipment",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Equipment",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Available",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValue: "Available")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Equipment",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Equipment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Equipment",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                table: "Equipment",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Equipment",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Equipment",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Equipment",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "Equipment",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CheckoutRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    User = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CheckedOutAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckoutRecords_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckoutRecords_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutRecords_EquipmentId",
                table: "CheckoutRecords",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutRecords_TeamId",
                table: "CheckoutRecords",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentCheckouts_Equipment_EquipmentID",
                table: "EquipmentCheckouts",
                column: "EquipmentID",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentCheckouts_Equipment_EquipmentID",
                table: "EquipmentCheckouts");

            migrationBuilder.DropTable(
                name: "CheckoutRecords");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "EquipmentCheckouts");

            migrationBuilder.DropColumn(
                name: "ReturnCondition",
                table: "EquipmentCheckouts");

            migrationBuilder.DropColumn(
                name: "ReturnDate",
                table: "EquipmentCheckouts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "LastUpdatedDate",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "EquipmentID",
                table: "EquipmentCheckouts",
                newName: "EquipmentId");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "EquipmentCheckouts",
                newName: "EquipmentID");

            migrationBuilder.RenameIndex(
                name: "IX_EquipmentCheckouts_EquipmentID",
                table: "EquipmentCheckouts",
                newName: "IX_EquipmentCheckouts_EquipmentId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsBlacklisted",
                table: "Teams",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Teams",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Teams",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentId",
                table: "EquipmentCheckouts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CheckedOutBy",
                table: "EquipmentCheckouts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReturnApprovedBy",
                table: "EquipmentCheckouts",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StorageLocation",
                table: "Equipment",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Equipment",
                type: "longtext",
                nullable: false,
                defaultValue: "Available",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Available")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Equipment",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CheckoutRecord",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    CheckedOutAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckoutRecord_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckoutRecord_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentCheckouts_EquipmentID",
                table: "EquipmentCheckouts",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutRecord_EquipmentId",
                table: "CheckoutRecord",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutRecord_TeamId",
                table: "CheckoutRecord",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentCheckouts_Equipment_EquipmentID",
                table: "EquipmentCheckouts",
                column: "EquipmentID",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentCheckouts_Equipment_EquipmentId",
                table: "EquipmentCheckouts",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id");
        }
    }
}
