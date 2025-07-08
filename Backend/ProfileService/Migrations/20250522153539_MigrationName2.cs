using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfileService.Migrations
{
    /// <inheritdoc />
    public partial class MigrationName2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Persons_PersonId1",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_PersonId1",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "PersonId1",
                table: "Images");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonId1",
                table: "Images",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_PersonId1",
                table: "Images",
                column: "PersonId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Persons_PersonId1",
                table: "Images",
                column: "PersonId1",
                principalTable: "Persons",
                principalColumn: "Id");
        }
    }
}
