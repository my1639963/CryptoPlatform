using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserColumes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "sys_user",
                type: "varchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "must_modify_pwd",
                table: "sys_user",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "password_algorithm",
                table: "sys_user",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PBKDF2-SHA256");

            migrationBuilder.AddColumn<DateTime>(
                name: "password_changed_at",
                table: "sys_user",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "password_version",
                table: "sys_user",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddForeignKey(
                name: "FK_sys_user_role_sys_role_role_id",
                table: "sys_user_role",
                column: "role_id",
                principalTable: "sys_role",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sys_user_role_sys_role_role_id",
                table: "sys_user_role");

            migrationBuilder.DropColumn(
                name: "must_modify_pwd",
                table: "sys_user");

            migrationBuilder.DropColumn(
                name: "password_algorithm",
                table: "sys_user");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                table: "sys_user");

            migrationBuilder.DropColumn(
                name: "password_version",
                table: "sys_user");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "sys_user",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(512)",
                oldMaxLength: 512);
        }
    }
}
