using System.Net;
using System.Net.Mail;
using System.Reflection;
using ERPServer.Application.Behaviors;
using ERPServer.Application.Mail;
using ERPServer.Domain.Mail;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERPServer.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEmail(configuration);

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

        /// <summary>
        /// Mail gondericisini kurar. SmtpHost bos birakilirsa gercek gonderici yerine
        /// sessizce dusuren bir gonderici baglanir; boyle bir kurulumda demo
        /// dogrulamasi da kendiliginden kapanir.
        /// </summary>
        private static IServiceCollection AddEmail(
            this IServiceCollection services, IConfiguration configuration)
        {
            IConfigurationSection section = configuration.GetSection(MailOptions.SectionName);
            services.Configure<MailOptions>(section);

            MailOptions options = section.Get<MailOptions>() ?? new MailOptions();

            var builder = services.AddFluentEmail(
                options.From,
                string.IsNullOrWhiteSpace(options.FromName) ? null : options.FromName);

            if (string.IsNullOrWhiteSpace(options.SmtpHost))
            {
                services.AddSingleton<FluentEmail.Core.Interfaces.ISender, NullEmailSender>();
                return services;
            }

            // Gercek mail sunuculari neredeyse her zaman kimlik dogrulama ve TLS ister;
            // yalnizca host ve port veren bir kurulum onlara baglanamaz.
            builder.AddSmtpSender(() =>
            {
                var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
                {
                    EnableSsl = options.UseSsl,
                };

                if (!string.IsNullOrWhiteSpace(options.Username))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(options.Username, options.Password);
                }

                return client;
            });

            return services;
        }
    }
}
