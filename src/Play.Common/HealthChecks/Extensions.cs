using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Play.Common.Settings;

namespace Play.Common.HealthChecks
{
    public static class Extensions
    {
        public static IHealthChecksBuilder AddMongoDbHealthCheck(this IHealthChecksBuilder builder)
        {
            return builder.Add(registration: new HealthCheckRegistration(
                    "MongoDb Health Check",
                    serviceProvider =>
                    {
                        IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
                        var mongoDbSettings = configuration
                                                                        .GetSection(nameof(MongoDbSettings))
                                                                        .Get<MongoDbSettings>();
                        var mongoClient = new MongoClient(mongoDbSettings.ConnectionString);
                        return new MongoDbHealthChecks(mongoClient);
                    },
                    HealthStatus.Unhealthy,
                    //tags to group health checks
                    new[] { "ready" },
                    TimeSpan.FromSeconds(5)
            ));
        }

        public static void MapPlayEconomyHealthChecks(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("ready")
            });
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
            {
                //we are returning false because we are not interested on the health status, but are only interested in receiving a response from the service
                Predicate = (check) => false
            });
        }

    }
}