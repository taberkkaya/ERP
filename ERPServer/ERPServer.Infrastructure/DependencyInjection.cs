using System.Reflection;
using ERPServer.Application.Services;
using ERPServer.Domain.Demo;
using ERPServer.Domain.Entities;
using ERPServer.Infrastructure.Context;
using ERPServer.Infrastructure.Demo;
using ERPServer.Infrastructure.Options;
using GenericRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace ERPServer.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>Demo türlerinin bulunduğu ad alanı; toplu tarama bunları atlıyor.</summary>
        private const string DemoNamespace = "ERPServer.Infrastructure.Demo";

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DemoOptions>(configuration.GetSection(DemoOptions.SectionName));

            // Bağlantı, isteğin jetonuna bakılarak çözülüyor: demo ziyaretçisi kendi
            // sandbox'ına, diğer herkes ana veritabanına gidiyor.
            services.AddHttpContextAccessor();
            services.AddScoped<IDemoContext, DemoContext>();
            services.AddScoped<ConnectionStringResolver>();

            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(
                    serviceProvider.GetRequiredService<ConnectionStringResolver>().Resolve());
            });

            services.AddScoped<IUnitOfWork>(srv => srv.GetRequiredService<ApplicationDbContext>());

            // Slot havuzu süreç geneli bir durum, bu yüzden singleton; arka plan
            // servisi de aynı örneği kullanıyor.
            services.AddSingleton<DemoSessionService>();
            services.AddSingleton<IDemoSessionService>(srv => srv.GetRequiredService<DemoSessionService>());
            services.AddHostedService<DemoHostedService>();

            services
                .AddIdentity<AppUser, IdentityRole<Guid>>(cfr =>
                {
                    cfr.Password.RequiredLength = 1;
                    cfr.Password.RequireNonAlphanumeric = false;
                    cfr.Password.RequireUppercase = false;
                    cfr.Password.RequireLowercase = false;
                    cfr.Password.RequireDigit = false;
                    cfr.SignIn.RequireConfirmedEmail = true;
                    cfr.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    cfr.Lockout.MaxFailedAccessAttempts = 3;
                    cfr.Lockout.AllowedForNewUsers = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.ConfigureOptions<JwtTokenOptionsSetup>();

            // Varsayilan sema acikca Bearer: AddIdentity varsayilani cereze
            // cekiyor ve semasi belirtilmemis her uc, jeton tasiyan bir istekte
            // bile giris sayfasina yonlendiriyordu.
            // AddIdentity yalnizca DefaultScheme'i degil DefaultAuthenticateScheme
            // ve DefaultChallengeScheme'i de cereze cekiyor; ucunu de acikca geri
            // aliyoruz, aksi halde semasi belirtilmemis uclar jeton tasiyan bir
            // istekte bile giris sayfasina yonlendiriyor.
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer();

            services.AddAuthorizationBuilder();

            services.Scan(action =>
            {
                action
                .FromAssemblies(Assembly.GetExecutingAssembly())
                // Demo türlerinin ömrü elle belirleniyor (havuz singleton, arka plan
                // servisi hosted); toplu tarama onları scoped'a çevirmemeli.
                .AddClasses(
                    classes => classes.Where(type => type.Namespace?.StartsWith(DemoNamespace, StringComparison.Ordinal) != true),
                    publicOnly: false)
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsMatchingInterface()
                .AsImplementedInterfaces()
                .WithScopedLifetime();
            });

            return services;
        }
    }
}
