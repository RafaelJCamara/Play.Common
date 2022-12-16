using GreenPipes;
using GreenPipes.Configurators;
using MassTransit;
using MassTransit.Definition;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Play.Common.MassTransit
{
    public static class Extensions
    {

        private const string RabbitMq = "RABBITMQ";
        private const string ServiceBus = "SERVICEBUS";

        public static IServiceCollection AddMassTransitWithessageBroker(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IRetryConfigurator> configureRetries = null)
        {
            var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            switch (serviceSettings.MessageBroker?.ToUpperInvariant())
            {
                case ServiceBus:
                    services.AddMassTransitWithServiceBus(configureRetries);
                    break;
                case RabbitMq:
                default:
                    services.AddMassTransitWithRabbitMq(configureRetries);
                    break;
            }
            return services;
        }

        public static IServiceCollection AddMassTransitWithRabbitMq(
            this IServiceCollection services,
            Action<IRetryConfigurator> configureRetries = null)
        {
            services
                .AddMassTransit(configure =>
                {
                    configure.AddConsumers(Assembly.GetEntryAssembly());
                    configure.UsingPlayEconomyRabbitMq(configureRetries);
                });

            services
                .AddMassTransitHostedService();

            return services;
        }

        public static IServiceCollection AddMassTransitWithServiceBus(
            this IServiceCollection services,
            Action<IRetryConfigurator> configureRetries = null)
        {
            services
                .AddMassTransit(configure =>
                {
                    configure.AddConsumers(Assembly.GetEntryAssembly());
                    configure.UsingPlayEconomyAzureServiceBus(configureRetries);
                });

            services
                .AddMassTransitHostedService();

            return services;
        }

        public static void UsingPlayEconomyMessageBroker(
            this IServiceCollectionBusConfigurator configure,
            IConfiguration configuration,
            Action<IRetryConfigurator> configureRetries = null)
        {
            var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            switch (serviceSettings.MessageBroker?.ToUpperInvariant())
            {
                case ServiceBus:
                    configure.UsingPlayEconomyAzureServiceBus(configureRetries);
                    break;
                case RabbitMq:
                default:
                    configure.UsingPlayEconomyRabbitMq(configureRetries);
                    break;
            }
        }

        public static void UsingPlayEconomyAzureServiceBus(
            this IServiceCollectionBusConfigurator configure,
            Action<IRetryConfigurator> configureRetries = null)
        {
            configure.UsingAzureServiceBus((context, configurator) =>
            {
                var configuration = context.GetService<IConfiguration>();
                var serviceBusSettings = configuration.GetSection(nameof(ServiceBusSettings)).Get<ServiceBusSettings>();
                var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                configurator.Host(serviceBusSettings.ConnectionString);

                configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings.ServiceName, false));
                if (configureRetries == null)
                {
                    configureRetries = (retryConfigurator) => retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                }

                configurator.UseMessageRetry(configureRetries);
            });
        }

        public static void UsingPlayEconomyRabbitMq(
            this IServiceCollectionBusConfigurator configure,
            Action<IRetryConfigurator> configureRetries = null)
        {
            configure.UsingRabbitMq((context, configurator) =>
            {
                var configuration = context.GetService<IConfiguration>();
                var rabbitMQSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
                var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                configurator.Host(rabbitMQSettings.Host);
                //helps us in the creation of rabbitMQ queues
                configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings.ServiceName, false));
                //this means that we are retrying if, by any chance, the consumer of the message could not consume it successfully
                //configurator.UseMessageRetry(retryConfigurator =>
                //{
                // it will try to consume the message 3 times, with a time difference between them of 5 seconds
                //    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                //});

                if (configureRetries == null)
                {
                    configureRetries = (retryConfigurator) => retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                }

                configurator.UseMessageRetry(configureRetries);
            });
        }

    }
}
