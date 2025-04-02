using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class seeded_data : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "Name", "Price", "SerialNumberId" },
                values: new object[] { 7, "Hehe Product", 200.0, 3 });

            migrationBuilder.InsertData(
                table: "SerialNumbers",
                columns: new[] { "Id", "ItemId", "Name" },
                values: new object[] { 3, 7, "heh710" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SerialNumbers",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
