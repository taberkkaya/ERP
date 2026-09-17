using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using ERPServer.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace ERPServer.Infrastructure.Demo;

/// <summary>
/// Bir sandbox veritabanını demo başlangıç durumuna getirir.
///
/// Veri, uygulamanın tüm akışını tek bakışta gösterecek şekilde kuruldu: alış
/// faturasıyla dolmuş hammadde stoğu, çalışmış bir üretim, satılmış mamul ve
/// karşılanması için eksik bileşen gerektiren açık bir sipariş. Böylece ziyaretçi
/// ihtiyaç planlamasını çalıştırdığında boş bir liste değil, gerçek bir sonuç
/// görüyor.
///
/// Faturalar Defter'de kesiliyor; burada yalnızca onların doğurduğu stok
/// hareketleri var, belge numaralarıyla birlikte.
/// </summary>
internal static class DemoDataSeeder
{
    public static async Task ResetAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        await WipeAsync(context, cancellationToken);
        await SeedAsync(context, cancellationToken);
    }

    /// <summary>
    /// Sandbox'ı boşaltır. Silme sırası yabancı anahtarlara göre: önce hareketler,
    /// sonra belgeler, en sonda tanımlar.
    /// </summary>
    public static async Task WipeAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        await context.StockMovements.ExecuteDeleteAsync(cancellationToken);
        await context.OrderDetails.ExecuteDeleteAsync(cancellationToken);
        await context.Orders.ExecuteDeleteAsync(cancellationToken);
        await context.Productions.ExecuteDeleteAsync(cancellationToken);
        await context.RecipeDetails.ExecuteDeleteAsync(cancellationToken);
        await context.Recipes.ExecuteDeleteAsync(cancellationToken);
        await context.Products.ExecuteDeleteAsync(cancellationToken);
        await context.Depots.ExecuteDeleteAsync(cancellationToken);
        await context.Customers.ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        // --- cariler -------------------------------------------------------
        Customer anadolu = new()
        {
            Name = "Anadolu Mobilya Sanayi A.Ş.",
            TaxDepartment = "Kadıköy",
            TaxNumber = "1234567890",
            City = "İstanbul",
            Town = "Kadıköy",
            Address = "Fikirtepe Mah. Sanayi Cad. No:14"
        };

        Customer ege = new()
        {
            Name = "Ege Ofis Sistemleri Ltd. Şti.",
            TaxDepartment = "Konak",
            TaxNumber = "2345678901",
            City = "İzmir",
            Town = "Konak",
            Address = "Akdeniz Mah. Gazi Bulvarı No:88"
        };

        Customer baskent = new()
        {
            Name = "Başkent Yapı Market A.Ş.",
            TaxDepartment = "Çankaya",
            TaxNumber = "3456789012",
            City = "Ankara",
            Town = "Çankaya",
            Address = "Kızılay Mah. Atatürk Bulvarı No:210"
        };

        Customer marmara = new()
        {
            Name = "Marmara Hammadde Tic. Ltd.",
            TaxDepartment = "Gebze",
            TaxNumber = "4567890123",
            City = "Kocaeli",
            Town = "Gebze",
            Address = "Güzeller OSB 3. Cad. No:7"
        };

        context.Customers.AddRange(anadolu, ege, baskent, marmara);

        // --- depolar -------------------------------------------------------
        Depot hammadde = new()
        {
            Name = "Hammadde Deposu",
            City = "Kocaeli",
            Town = "Gebze",
            Address = "Güzeller OSB 1. Cad. No:3"
        };

        Depot uretim = new()
        {
            Name = "Üretim Deposu",
            City = "Kocaeli",
            Town = "Gebze",
            Address = "Güzeller OSB 1. Cad. No:3 / A Blok"
        };

        Depot merkez = new()
        {
            Name = "Merkez Depo",
            City = "İstanbul",
            Town = "Ataşehir",
            Address = "Barbaros Mah. Lojistik Sok. No:5"
        };

        context.Depots.AddRange(hammadde, uretim, merkez);

        // --- ürünler -------------------------------------------------------
        Product sandalye = Mamul("Ofis Sandalyesi ERGO-100");
        Product masa = Mamul("Toplantı Masası 180 cm");
        Product kitaplik = Mamul("Kitaplık 5 Raflı");

        Product panel = YariMamul("Ahşap Panel 18 mm");
        Product ayak = YariMamul("Metal Ayak Takımı");
        Product amortisor = YariMamul("Gaz Amortisör");
        Product tekerlek = YariMamul("Tekerlek Seti (5 li)");
        Product minder = YariMamul("Sünger Minder");
        Product kumas = YariMamul("Kumaş Kaplama (m2)");
        Product vida = YariMamul("Vida Seti M8");
        Product bant = YariMamul("Kenar Bandı (m)");

        context.Products.AddRange(
            sandalye, masa, kitaplik,
            panel, ayak, amortisor, tekerlek, minder, kumas, vida, bant);

        // --- reçeteler -----------------------------------------------------
        context.Recipes.AddRange(
            Recipe(sandalye,
                (ayak, 1m), (amortisor, 1m), (tekerlek, 1m),
                (minder, 2m), (kumas, 1.5m), (vida, 1m)),
            Recipe(masa,
                (panel, 2m), (ayak, 2m), (vida, 2m), (bant, 6m)),
            Recipe(kitaplik,
                (panel, 5m), (vida, 3m), (bant, 10m)));

        // --- alış faturası: hammadde stoğu ---------------------------------
        (Product Product, decimal Quantity, decimal Price)[] purchaseLines =
        [
            (panel, 120m, 480m),
            (ayak, 60m, 950m),
            (amortisor, 40m, 320m),
            (tekerlek, 35m, 180m),
            (minder, 90m, 140m),
            (kumas, 70m, 260m),
            (vida, 200m, 45m),
            (bant, 300m, 12m)
        ];

        // Faturanın kendisi Defter'de; buradaki kimlik onun karşılığı. Sabit
        // değil üretilmiş bir Guid: her sandbox kendi hareketlerini taşıyor.
        Guid purchaseDocumentId = Guid.NewGuid();

        foreach ((Product product, decimal quantity, decimal price) in purchaseLines)
        {
            context.StockMovements.Add(new StockMovement
            {
                Source = StockMovementSourceEnum.PurchaseInvoice,
                ExternalDocumentId = purchaseDocumentId,
                ExternalDocumentNumber = "ALS2026000001",
                ProductId = product.Id,
                DepotId = hammadde.Id,
                NumberOfEntries = quantity,
                Price = price
            });
        }

        // --- üretim: 12 adet sandalye --------------------------------------
        // Reçetedeki bileşenler hammadde deposundan düşülüyor, mamul üretim
        // deposuna giriyor; maliyeti de tüketilen bileşenlerin toplamı.
        const decimal producedQuantity = 12m;

        (Product Product, decimal PerUnit, decimal Price)[] consumed =
        [
            (ayak, 1m, 950m),
            (amortisor, 1m, 320m),
            (tekerlek, 1m, 180m),
            (minder, 2m, 140m),
            (kumas, 1.5m, 260m),
            (vida, 1m, 45m)
        ];

        Production production = new()
        {
            ProductId = sandalye.Id,
            DepotId = uretim.Id,
            Quantity = producedQuantity,
            CreatedAt = DateTime.Now.AddDays(-12)
        };

        decimal unitCost = 0;

        foreach ((Product product, decimal perUnit, decimal price) in consumed)
        {
            unitCost += perUnit * price;

            context.StockMovements.Add(new StockMovement
            {
                Source = StockMovementSourceEnum.Production,
                ProductionId = production.Id,
                ProductId = product.Id,
                DepotId = hammadde.Id,
                NumberOfOutputs = perUnit * producedQuantity,
                Price = price
            });
        }

        context.StockMovements.Add(new StockMovement
        {
            Source = StockMovementSourceEnum.Production,
            ProductionId = production.Id,
            ProductId = sandalye.Id,
            DepotId = uretim.Id,
            NumberOfEntries = producedQuantity,
            Price = unitCost
        });

        context.Productions.Add(production);

        // --- tamamlanmış sipariş ve onu kapatan satış ----------------------
        Order completedOrder = new()
        {
            CustomerId = anadolu.Id,
            OrderNumberYear = today.Year,
            OrderNumber = 1,
            Date = today.AddDays(-15),
            DeliveryDate = today.AddDays(-3),
            Status = OrderStatusEnum.Completed,
            Details =
            [
                new OrderDetail { ProductId = sandalye.Id, Quantity = 5m, Price = 3450m }
            ]
        };

        // Siparişi kapatan satış faturası da Defter'de kesilmişti; buradaki iz
        // onun stoktan düşürdüğü beş sandalye.
        context.StockMovements.Add(new StockMovement
        {
            Source = StockMovementSourceEnum.SalesInvoice,
            ExternalDocumentId = Guid.NewGuid(),
            ExternalDocumentNumber = "STS2026000001",
            ProductId = sandalye.Id,
            DepotId = uretim.Id,
            NumberOfOutputs = 5m,
            Price = 3450m
        });

        context.Orders.Add(completedOrder);

        // --- açık siparişler -----------------------------------------------
        // Bu siparişin ihtiyaç planı boş çıkmıyor: masa ve kitaplık için gereken
        // ahşap panel, metal ayak ve kenar bandı stoktan fazla.
        context.Orders.Add(new Order
        {
            CustomerId = anadolu.Id,
            OrderNumberYear = today.Year,
            OrderNumber = 2,
            Date = today.AddDays(-8),
            DeliveryDate = today.AddDays(6),
            Status = OrderStatusEnum.Pending,
            Details =
            [
                new OrderDetail { ProductId = sandalye.Id, Quantity = 20m, Price = 3450m },
                new OrderDetail { ProductId = masa.Id, Quantity = 30m, Price = 6200m },
                new OrderDetail { ProductId = kitaplik.Id, Quantity = 15m, Price = 2100m }
            ]
        });

        context.Orders.Add(new Order
        {
            CustomerId = ege.Id,
            OrderNumberYear = today.Year,
            OrderNumber = 3,
            Date = today.AddDays(-4),
            DeliveryDate = today.AddDays(12),
            Status = OrderStatusEnum.Pending,
            Details =
            [
                new OrderDetail { ProductId = sandalye.Id, Quantity = 8m, Price = 3390m }
            ]
        });

        context.Orders.Add(new Order
        {
            CustomerId = baskent.Id,
            OrderNumberYear = today.Year,
            OrderNumber = 4,
            Date = today.AddDays(-2),
            DeliveryDate = today.AddDays(20),
            Status = OrderStatusEnum.Pending,
            Details =
            [
                new OrderDetail { ProductId = kitaplik.Id, Quantity = 25m, Price = 2050m }
            ]
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Product Mamul(string name) =>
        new() { Name = name, ProductType = ProductTypeEnum.Product };

    private static Product YariMamul(string name) =>
        new() { Name = name, ProductType = ProductTypeEnum.SemiProduct };

    private static Recipe Recipe(Product head, params (Product Product, decimal Quantity)[] components)
    {
        Recipe recipe = new() { ProductId = head.Id, Details = [] };

        foreach ((Product product, decimal quantity) in components)
        {
            recipe.Details.Add(new RecipeDetail
            {
                RecipeId = recipe.Id,
                ProductId = product.Id,
                Quantity = quantity
            });
        }

        return recipe;
    }
}
