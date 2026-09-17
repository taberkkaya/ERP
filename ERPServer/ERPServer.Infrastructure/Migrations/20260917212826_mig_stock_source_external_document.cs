using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig_stock_source_external_document : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Invoices_InvoiceId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "InvoiceDetails");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.RenameColumn(
                name: "InvoiceId",
                table: "StockMovements",
                newName: "ExternalDocumentId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_InvoiceId",
                table: "StockMovements",
                newName: "IX_StockMovements_ExternalDocumentId");

            migrationBuilder.AddColumn<string>(
                name: "ExternalDocumentNumber",
                table: "StockMovements",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            // Varsayilan 1 (Elle): 0 gecerli bir StockMovementSourceEnum degeri degil
            // ve FromValue okurken hata verirdi.
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "StockMovements",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Mevcut satirlarin kaynagi elimizdeki izlerden cikariliyor: uretim
            // kimligi tasiyanlar uretimden, eski InvoiceId'si (artik
            // ExternalDocumentId) dolu olanlar faturadan geliyor. Giris mi cikis
            // mi oldugu alis/satis ayrimini veriyor.
            migrationBuilder.Sql("""
                UPDATE StockMovements SET Source = 2 WHERE ProductionId IS NOT NULL;

                UPDATE StockMovements
                SET Source = CASE WHEN NumberOfEntries > 0 THEN 3 ELSE 4 END
                WHERE ProductionId IS NULL AND ExternalDocumentId IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalDocumentNumber",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "StockMovements");

            migrationBuilder.RenameColumn(
                name: "ExternalDocumentId",
                table: "StockMovements",
                newName: "InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_ExternalDocumentId",
                table: "StockMovements",
                newName: "IX_StockMovements_InvoiceId");

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "money", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(7,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceDetails_Depots_DepotId",
                        column: x => x.DepotId,
                        principalTable: "Depots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceDetails_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDetails_DepotId",
                table: "InvoiceDetails",
                column: "DepotId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDetails_InvoiceId",
                table: "InvoiceDetails",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDetails_ProductId",
                table: "InvoiceDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Invoices_InvoiceId",
                table: "StockMovements",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");
        }
    }
}
