using GreenPipes;
using MassTransit;
using MassTransit.Definition;
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
        public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services)
        {
            services
                .AddMassTransit(configure =>
                {
                    configure.AddConsumers(Assembly.GetEntryAssembly());

                    configure.UsingRabbitMq((context, configurator) =>
                    {
                        var configuration = context.GetService<IConfiguration>();
                        var rabbitMQSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
                        var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                        configurator.Host(rabbitMQSettings.Host);
                        //helps us in the creation of rabbitMQ queues
                        configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings.ServiceName, false));
                        //this means that we are retrying if, by any chance, the consumer of the message could not consume it successfully
                        configurator.UseMessageRetry(retryConfigurator =>
                        {
                            //it will try to consume the message 3 times, with a time difference between them of 5 seconds
                            retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                        });
                    });
                });

            services
                .AddMassTransitHostedService();

            return services;
        }
    }
}
