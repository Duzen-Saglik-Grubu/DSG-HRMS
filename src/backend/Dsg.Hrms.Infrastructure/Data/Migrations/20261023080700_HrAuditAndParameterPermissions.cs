using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class HrAuditAndParameterPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permission",
                columns: new[] { "permission", "role_id" },
                values: new object[,]
                {
                    { "audit.log.view", 2L },
                    { "system.parameter.update", 2L },
                    { "system.parameter.view", 2L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "audit.log.view", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "system.parameter.update", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "system.parameter.view", 2L });
        }
    }
}
