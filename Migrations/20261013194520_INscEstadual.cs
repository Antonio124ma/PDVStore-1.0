using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDVStore.Migrations
{
    /// <inheritdoc />
    public partial class INscEstadual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InscricaoEstadual",
                table: "Fornecedores",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InscricaoEstadual",
                table: "Fornecedores");
        }
    }
}
