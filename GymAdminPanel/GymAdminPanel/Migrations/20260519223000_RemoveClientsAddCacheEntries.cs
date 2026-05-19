using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymAdminPanel.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClientsAddCacheEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "Clients";""");

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "CacheEntries" (
                    "CacheKey" TEXT NOT NULL CONSTRAINT "PK_CacheEntries" PRIMARY KEY,
                    "PayloadJson" TEXT NOT NULL,
                    "CachedAt" TEXT NOT NULL
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "CacheEntries";""");

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Clients" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Clients" PRIMARY KEY AUTOINCREMENT,
                    "Email" TEXT NOT NULL,
                    "FirstName" TEXT NOT NULL,
                    "IsActive" INTEGER NOT NULL,
                    "LastName" TEXT NOT NULL,
                    "RegistrationDate" TEXT NOT NULL
                );
                """);
        }
    }
}
