using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HierarchScraper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVacancyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalDataJson",
                table: "Vacancies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplyDate",
                table: "Vacancies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplyLink",
                table: "Vacancies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Vacancies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContractType",
                table: "Vacancies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vacancies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "Vacancies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Vacancies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostedDateRaw",
                table: "Vacancies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemotePolicy",
                table: "Vacancies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Salary",
                table: "Vacancies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalDataJson",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "ApplyDate",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "ApplyLink",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "PostedDateRaw",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "RemotePolicy",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Salary",
                table: "Vacancies");
        }
    }
}
