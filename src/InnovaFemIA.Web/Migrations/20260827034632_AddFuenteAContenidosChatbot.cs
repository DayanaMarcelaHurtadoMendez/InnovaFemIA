using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovaFemIA.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFuenteAContenidosChatbot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Descripcion de Familias ya existe en la base de datos

            migrationBuilder.AddColumn<string>(
                name: "EnlaceFuente",
                table: "ContenidosChatbot",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NombreFuente",
                table: "ContenidosChatbot",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Tablas BoletinesSipsaInfo y PreciosMayoristasSipsa ya existen en la base de datos
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tablas BoletinesSipsaInfo y PreciosMayoristasSipsa ya existen en la base de datos

            // Descripcion de Familias ya existe en la base de datos

            migrationBuilder.DropColumn(
                name: "EnlaceFuente",
                table: "ContenidosChatbot");

            migrationBuilder.DropColumn(
                name: "NombreFuente",
                table: "ContenidosChatbot");
        }
    }
}
