using System.Security.Cryptography;
using System.Text;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using ERPServer.Domain.Integration;
using ERPServer.Domain.Repository;
using GenericRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TS.Result;

namespace ERPServer.WebAPI.Modules;

/// <summary>
/// Defter'in konuştuğu uçlar.
///
/// Fatura ve cari Defter'de, stok ve depo burada. Defter bir fatura onayladığında
/// doğan stok hareketini buraya yazıyor; ürün ve depo listelerini de buradan
/// okuyor, çünkü ikisinin de sahibi bu uygulama.
///
/// Kimlik doğrulaması JWT değil paylaşılan bir anahtar: çağıran bir sunucu,
/// oturum açmış bir kişi değil. Anahtar tanımlanmamışsa uçlar hiç eşlenmiyor —
/// açık kapı bırakmak, internetten herkesin stok yazabilmesi demek olurdu.
/// </summary>
public static class IntegrationModule
{
    private const string KeyHeader = "X-Erp-Key";

    public static void RegisterIntegrationRoutes(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/integration").WithTags("integration").AllowAnonymous();

        // --- okuma: Defter fatura keserken ürünü ve depoyu buradan seçiyor ---

        group.MapGet("products", async (
            HttpContext http,
            IProductRepository productRepository,
            IStockMovementRepository stockMovementRepository,
            IOptions<IntegrationOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (!IsAuthorized(http, options.Value)) return Results.Unauthorized();

            List<Product> products = await productRepository.GetAll()
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            // Stok tek sorguda toplanıyor: ürün başına ayrı sorgu, katalog
            // büyüdükçe Defter'in fatura ekranını yavaşlatırdı.
            var stocks = await stockMovementRepository.GetAll()
                .GroupBy(p => p.ProductId)
                .Select(g => new { ProductId = g.Key, Stock = g.Sum(s => s.NumberOfEntries - s.NumberOfOutputs) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);

            List<IntegrationProduct> response = products
                .Select(p => new IntegrationProduct(
                    p.Id,
                    p.Name,
                    p.ProductType.Name,
                    stocks.TryGetValue(p.Id, out decimal stock) ? stock : 0))
                .ToList();

            return Results.Ok((Result<List<IntegrationProduct>>)response);
        }).Produces<Result<List<IntegrationProduct>>>();

        group.MapGet("depots", async (
            HttpContext http,
            IDepotRepository depotRepository,
            IOptions<IntegrationOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (!IsAuthorized(http, options.Value)) return Results.Unauthorized();

            List<IntegrationDepot> response = await depotRepository.GetAll()
                .OrderBy(p => p.Name)
                .Select(p => new IntegrationDepot(p.Id, p.Name))
                .ToListAsync(cancellationToken);

            return Results.Ok((Result<List<IntegrationDepot>>)response);
        }).Produces<Result<List<IntegrationDepot>>>();

        // --- yazma: onaylanan fatura stok hareketine dönüşüyor ---

        group.MapPost("stock-movements", async (
            StockMovementRequest request,
            HttpContext http,
            IStockMovementRepository stockMovementRepository,
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            IOptions<IntegrationOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (!IsAuthorized(http, options.Value)) return Results.Unauthorized();

            if (request.DocumentId == Guid.Empty)
                return Results.BadRequest(Result<string>.Failure(400, ["Belge kimliği geçersiz."]));

            if (request.Lines is null || request.Lines.Count == 0)
                return Results.BadRequest(Result<string>.Failure(400, ["Belgede kalem yok."]));

            bool isPurchase = request.Direction == StockDirection.In;

            // Aynı belge yeniden gönderilebilir: Defter'de fatura düzeltilmiş
            // olabilir. Eski hareketler silinip yenileri yazılıyor, böylece
            // yeniden gönderim stoğu ikiye katlamıyor.
            List<StockMovement> existing = await stockMovementRepository
                .Where(p => p.ExternalDocumentId == request.DocumentId)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0) stockMovementRepository.DeleteRange(existing);

            List<StockMovement> movements = request.Lines
                .Select(line => new StockMovement
                {
                    ProductId = line.ProductId,
                    DepotId = line.DepotId,
                    NumberOfEntries = isPurchase ? line.Quantity : 0,
                    NumberOfOutputs = isPurchase ? 0 : line.Quantity,
                    Price = line.Price,
                    Source = isPurchase
                        ? StockMovementSourceEnum.PurchaseInvoice
                        : StockMovementSourceEnum.SalesInvoice,
                    ExternalDocumentId = request.DocumentId,
                    ExternalDocumentNumber = request.DocumentNumber
                })
                .ToList();

            await stockMovementRepository.AddRangeAsync(movements, cancellationToken);

            // Satış faturası bir siparişe bağlıysa sipariş tamamlanmış sayılır.
            // Bu kural eskiden fatura işleyicisindeydi; fatura taşınınca kural da
            // onunla birlikte buraya geldi.
            if (request.OrderId is { } orderId && !isPurchase)
            {
                Order? order = await orderRepository
                    .GetByExpressionWithTrackingAsync(p => p.Id == orderId, cancellationToken);

                if (order is not null) order.Status = OrderStatusEnum.Completed;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Results.Ok((Result<string>)$"{movements.Count} stok hareketi işlendi.");
        }).Produces<Result<string>>();

        // Defter'de fatura silindiğinde onun doğurduğu hareketler de gider;
        // aksi hâlde stokta karşılığı olmayan bir giriş kalırdı.
        group.MapDelete("stock-movements/{documentId:guid}", async (
            Guid documentId,
            HttpContext http,
            IStockMovementRepository stockMovementRepository,
            IUnitOfWork unitOfWork,
            IOptions<IntegrationOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (!IsAuthorized(http, options.Value)) return Results.Unauthorized();

            List<StockMovement> movements = await stockMovementRepository
                .Where(p => p.ExternalDocumentId == documentId)
                .ToListAsync(cancellationToken);

            if (movements.Count == 0) return Results.Ok((Result<string>)"Silinecek hareket yok.");

            stockMovementRepository.DeleteRange(movements);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Results.Ok((Result<string>)$"{movements.Count} stok hareketi silindi.");
        }).Produces<Result<string>>();
    }

    private static bool IsAuthorized(HttpContext http, IntegrationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey)) return false;

        string provided = http.Request.Headers[KeyHeader].ToString();
        if (provided.Length == 0) return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(options.ApiKey));
    }

    /// <summary>Belgenin stoğa girip çıkmadığı.</summary>
    public enum StockDirection
    {
        In = 1,
        Out = 2
    }

    public sealed record StockMovementLine(Guid ProductId, Guid DepotId, decimal Quantity, decimal Price);

    /// <param name="OrderId">Satış faturası bir siparişe bağlıysa o siparişin kimliği.</param>
    public sealed record StockMovementRequest(
        Guid DocumentId,
        string? DocumentNumber,
        StockDirection Direction,
        Guid? OrderId,
        List<StockMovementLine>? Lines);

    public sealed record IntegrationProduct(Guid Id, string Name, string ProductType, decimal Stock);

    public sealed record IntegrationDepot(Guid Id, string Name);
}
