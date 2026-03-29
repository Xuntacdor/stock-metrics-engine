using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Api.Migrations
{
    public partial class AddFullTextSearchNews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create full-text catalog
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'FtCatalog') CREATE FULLTEXT CATALOG FtCatalog AS DEFAULT;");

            // Create full-text index on NewsArticles(Title, Summary)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.fulltext_indexes fi
                    JOIN sys.objects o ON fi.object_id = o.object_id
                    WHERE o.name = 'NewsArticles'
                )
                CREATE FULLTEXT INDEX ON NewsArticles(Title, Summary)
                KEY INDEX PK_NewsArticles
                ON FtCatalog
                WITH CHANGE_TRACKING AUTO;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.fulltext_indexes fi JOIN sys.objects o ON fi.object_id = o.object_id WHERE o.name = 'NewsArticles') DROP FULLTEXT INDEX ON NewsArticles;");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'FtCatalog') DROP FULLTEXT CATALOG FtCatalog;");
        }
    }
}
