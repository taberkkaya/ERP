using System.Reflection;
using ERPServer.Application.Behaviors;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ERPServer.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            // Mapster yapilandirmasi bir kez derlenip paylasiliyor; IRegister
            // uygulayan siniflar (MappingRegister) taramayla bulunuyor.
            TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;
            config.Scan(assembly);

            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();

            services.AddMediatR(conf =>
            {
                conf.RegisterServicesFromAssemblies(assembly);
                conf.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
