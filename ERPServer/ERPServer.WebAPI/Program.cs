using System.Threading.RateLimiting;
using ERPServer.Application;
using ERPServer.Infrastructure;
using ERPServer.WebAPI.Middlewares;
using ERPServer.WebAPI.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// İmzalama anahtarı üretimde zorunlu. Depodaki örnek anahtarla yayına çıkmak,
// herkesin kendine jeton üretebilmesi demek.
string? jwtSecret = builder.Configuration["Jwt:SecretKey"];

if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "Jwt__SecretKey tanımlı değil. Production'da bu değer ortam değişkeniyle verilmelidir.");
}

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// İstemci ve API aynı origin'den servis edildiğinde (Caddy /api'yi yönlendiriyor)
// bu liste boş kalır ve hiçbir cross-origin isteğe gerek olmaz.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            return;
        }

        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Demo oturumu açmak sandbox sıfırlama maliyeti taşıyor; tek bir adresin havuzu
// arka arkaya tüketmesini engelliyor.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("demo-start", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Kod gönderimi mail kutusuna dokunuyor: tek bir adresin başkasını rahatsız
    // etmek için kullanılmasını engelliyor.
    options.AddPolicy("demo-code", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5)
            }));
});

builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setup =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Aşağıdaki kutuya **_YALNIZCA_** JWT bearer jetonunu yazın.",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    setup.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Konteynerde TLS'i önündeki ters vekil sonlandırıyor; burada yönlendirme
    // yapmak sonsuz döngüye yol açardı.
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

// Kimlik doğrulamadan sonra: kotayı işleyebilmek için jetondaki oturum
// bilgisinin çözülmüş olması gerekiyor.
app.UseMiddleware<DemoSessionMiddleware>();

app.MapControllers();
app.RegisterDemoRoutes();

// Defter'in konustugu uclar. Anahtar tanimlanmamissa istekleri reddediyor;
// tek basina calisan bir kurulumda entegrasyon kapali kalir.
app.RegisterIntegrationRoutes();

// Coolify dağıtımda yeni konteynere ne zaman geçeceğini buradan anlıyor.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await app.PrepareDatabaseAsync();

app.Run();
