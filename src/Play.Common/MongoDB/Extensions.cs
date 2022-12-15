using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Play.Common.Repository;
using Play.Common.Settings;

namespace Play.Common.MongoDB
{
    public static class Extensions
    {
        public static IServiceCollection AddMongo(this IServiceCollection services)
        {
            /*
                Registering this serializers, allow us to have more readable representations of the field serializers defined
                In this case, according to the commands specified down below, we are intending to obtain more readable representations for GUIDs and for DateTimeOffset
             */
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

            // We use service provider whenever we have a complex way of building the dependency that is going to be mapped
            services.AddSingleton(serviceProvider =>
            {
                // We can use the nameof here because the name of the class is the same as the one presented in the configuration
                var configuration = serviceProvider.GetService<IConfiguration>();
                var mongoSettings = configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
                var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                var mongoClient = new MongoClient(mongoSettings.ConnectionString);
                return mongoClient.GetDatabase(serviceSettings.ServiceName);
            });

            return services;
        }

        public static IServiceCollection AddMongoRepository<T>(this IServiceCollection services, string collectionName) where T : IEntity
        {
            services.AddScoped<IRepository<T>>(serviceProvider =>
            {
                /*
                    By doing this, we are asking to the current service container for this dependency
                    The dependency must be defined previously to this
                 */
                var database = serviceProvider.GetService<IMongoDatabase>();
                return new MongoRepository<T>(database, collectionName);
            });
            return services;
        }


    }
}
