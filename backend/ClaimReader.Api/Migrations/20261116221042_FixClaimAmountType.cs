using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimReader.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixClaimAmountType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Claims",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.Sql(
                "ALTER TABLE \"Claims\" ALTER COLUMN \"ClaimAmount\" TYPE numeric(12,2) USING \"ClaimAmount\"::numeric(12,2);");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_CreatedAt",
                table: "Claims",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Claims_CreatedAt",
                table: "Claims");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Claims",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimAmount",
                table: "Claims",
                type: "text",
                precision: 12,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
