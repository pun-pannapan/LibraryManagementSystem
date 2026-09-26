using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowingWorkflowAndBookLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DueAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BorrowedAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedByUserId",
                table: "BorrowTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("UPDATE [BorrowTransactions] SET [AssignedAtUtc] = [BorrowedAtUtc] WHERE [BorrowedAtUtc] IS NOT NULL;");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedByUserId",
                table: "BorrowTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedByUserId",
                table: "BorrowTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("UPDATE [BorrowTransactions] SET [RequestedAtUtc] = [CreatedAtUtc] WHERE [RequestedAtUtc] = '0001-01-01T00:00:00.0000000';");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnRequestedAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Books",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShelfCode",
                table: "Books",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowTransactions_Status_CreatedAtUtc",
                table: "BorrowTransactions",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowTransactions_UserId_Status",
                table: "BorrowTransactions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Books_ShelfCode",
                table: "Books",
                column: "ShelfCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BorrowTransactions_Status_CreatedAtUtc",
                table: "BorrowTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BorrowTransactions_UserId_Status",
                table: "BorrowTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Books_ShelfCode",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "BorrowTransactions");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                table: "BorrowTransactions");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "BorrowTransactions");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "BorrowTransactions");

            migrationBuilder.DropColumn(
                name: "RejectedAtUtc",
                table: "BorrowTransactions");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "BorrowTransactions");

            migrationBuilder.DropColumn(
                name: "RequestedAtUtc",
                table: "BorrowTransactions");

            migrationBuilder.DropColumn(
                name: "ReturnRequestedAtUtc",
                table: "BorrowTransactions");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "ShelfCode",
                table: "Books");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "BorrowedAtUtc",
                table: "BorrowTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
