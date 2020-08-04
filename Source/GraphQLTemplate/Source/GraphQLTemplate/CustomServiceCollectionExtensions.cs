namespace GraphQLTemplate
{
    using System;
#if ResponseCompression
    using System.IO.Compression;
#endif
    using System.Linq;
    using Boxed.AspNetCore;
#if (Authorization || CORS)
    using GraphQLTemplate.Constants;
    using GraphQLTemplate.Directives;
#endif
    using GraphQLTemplate.Options;
    using GraphQLTemplate.Schemas;
    using GraphQLTemplate.Types;
    using HotChocolate;
#if PersistedQueries
    using HotChocolate.Execution;
#endif
    using HotChocolate.Execution.Configuration;
    using HotChocolate.Subscriptions;
    using HotChocolate.Types;
    using Microsoft.AspNetCore.Builder;
#if (!ForwardedHeaders && HostFiltering)
    using Microsoft.AspNetCore.HostFiltering;
#endif
#if ResponseCompression
    using Microsoft.AspNetCore.ResponseCompression;
#endif
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
#if PersistedQueries
    using StackExchange.Redis;
#endif

    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods which extend ASP.NET Core services.
    /// </summary>
    internal static class CustomServiceCollectionExtensions
    {
        /// <summary>
        /// Configures caching for the application. Registers the <see cref="IDistributedCache"/> and
        /// <see cref="IMemoryCache"/> types with the services collection or IoC container. The
        /// <see cref="IDistributedCache"/> is intended to be used in cloud hosted scenarios where there is a shared
        /// cache, which is shared between multiple instances of the application. Use the <see cref="IMemoryCache"/>
        /// otherwise.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with caching services added.</returns>
        public static IServiceCollection AddCustomCaching(this IServiceCollection services) =>
            services
                .AddMemoryCache()
                // Adds IDistributedCache which is a distributed cache shared between multiple servers. This adds a
                // default implementation of IDistributedCache which is not distributed. You probably want to use the
                // Redis cache provider by calling AddDistributedRedisCache.
                .AddDistributedMemoryCache();
#if CORS

        /// <summary>
        /// Add cross-origin resource sharing (CORS) services and configures named CORS policies (See
        /// https://docs.asp.net/en/latest/security/cors.html).
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with caching services added.</returns>
        public static IServiceCollection AddCustomCors(this IServiceCollection services) =>
            services.AddCors(
                options =>
                    // Create named CORS policies here which you can consume using application.UseCors("PolicyName")
                    // or a [EnableCors("PolicyName")] attribute on your controller or action.
                    options.AddPolicy(
                        CorsPolicyName.AllowAny,
                        x => x
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()));
#endif

        /// <summary>
        /// Configures the settings by binding the contents of the appsettings.json file to the specified Plain Old CLR
        /// Objects (POCO) and adding <see cref="IOptions{T}"/> objects to the services collection.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The services with caching services added.</returns>
        public static IServiceCollection AddCustomOptions(
            this IServiceCollection services,
            IConfiguration configuration) =>
            services
                // ConfigureAndValidateSingleton registers IOptions<T> and also T as a singleton to the services collection.
                .ConfigureAndValidateSingleton<ApplicationOptions>(configuration)
                .ConfigureAndValidateSingleton<CacheProfileOptions>(configuration.GetSection(nameof(ApplicationOptions.CacheProfiles)))
#if ResponseCompression
                .ConfigureAndValidateSingleton<CompressionOptions>(configuration.GetSection(nameof(ApplicationOptions.Compression)))
#endif
#if ForwardedHeaders
                .ConfigureAndValidateSingleton<ForwardedHeadersOptions>(configuration.GetSection(nameof(ApplicationOptions.ForwardedHeaders)))
                .Configure<ForwardedHeadersOptions>(
                    options =>
                    {
                        options.KnownNetworks.Clear();
                        options.KnownProxies.Clear();
                    })
#elif HostFiltering
                .ConfigureAndValidateSingleton<HostFilteringOptions>(configuration.GetSection(nameof(ApplicationOptions.HostFiltering)))
#endif
                .ConfigureAndValidateSingleton<QueryExecutionOptions>(configuration.GetSection(nameof(ApplicationOptions.GraphQL)))
#if PersistedQueries
                .ConfigureAndValidateSingleton<RedisOptions>(configuration.GetSection(nameof(ApplicationOptions.Redis)))
#endif
                .ConfigureAndValidateSingleton<KestrelServerOptions>(configuration.GetSection(nameof(ApplicationOptions.Kestrel)));
#if ResponseCompression

        /// <summary>
        /// Adds dynamic response compression to enable GZIP compression of responses. This is turned off for HTTPS
        /// requests by default to avoid the BREACH security vulnerability.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The services with caching services added.</returns>
        public static IServiceCollection AddCustomResponseCompression(
            this IServiceCollection services,
            IConfiguration configuration) =>
            services
                .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
                .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
                .AddResponseCompression(
                    options =>
                    {
                        // Add additional MIME types (other than the built in defaults) to enable GZIP compression for.
                        var customMimeTypes = configuration
                            .GetSection(nameof(ApplicationOptions.Compression))
                            .Get<CompressionOptions>()
                            ?.MimeTypes ?? Enumerable.Empty<string>();
                        options.MimeTypes = customMimeTypes.Concat(ResponseCompressionDefaults.MimeTypes);

                        options.Providers.Add<BrotliCompressionProvider>();
                        options.Providers.Add<GzipCompressionProvider>();
                    });
#endif

        /// <summary>
        /// Add custom routing settings which determines how URL's are generated.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with caching services added.</returns>
        public static IServiceCollection AddCustomRouting(this IServiceCollection services) =>
            services.AddRouting(options => options.LowercaseUrls = true);
#if HttpsEverywhere

        /// <summary>
        /// Adds the Strict-Transport-Security HTTP header to responses. This HTTP header is only relevant if you are
        /// using TLS. It ensures that content is loaded over HTTPS and refuses to connect in case of certificate
        /// errors and warnings.
        /// See https://developer.mozilla.org/en-US/docs/Web/Security/HTTP_strict_transport_security and
        /// http://www.troyhunt.com/2015/06/understanding-http-strict-transport.html
        /// Note: Including subdomains and a minimum maxage of 18 weeks is required for preloading.
        /// Note: You can refer to the following article to clear the HSTS cache in your browser
        /// (See http://classically.me/blogs/how-clear-hsts-settings-major-browsers).
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with caching services added.</returns>
        public static IServiceCollection AddCustomStrictTransportSecurity(this IServiceCollection services) =>
            services
                .AddHsts(
                    options =>
                    {
                        // Preload the HSTS HTTP header for better security. See https://hstspreload.org/
#if HstsPreload
                        options.IncludeSubDomains = true;
                        options.MaxAge = TimeSpan.FromSeconds(31536000); // 1 Year
                        options.Preload = true;
#else
                        // options.IncludeSubDomains = true;
                        // options.MaxAge = TimeSpan.FromSeconds(31536000); // 1 Year
                        // options.Preload = true;
#endif
                    });
#endif
#if HealthCheck

        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services) =>
            services
                .AddHealthChecks()
                // Add health checks for external dependencies here. See https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks
                .Services;
#endif
#if Authorization

        /// <summary>
        /// Add GraphQL authorization (See https://github.com/graphql-dotnet/authorization).
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with caching services added.</returns>
        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services) =>
            services
                .AddAuthorization(options => options
                    .AddPolicy(AuthorizationPolicyName.Admin, x => x.RequireAuthenticatedUser()));
#endif
#if PersistedQueries

        public static IServiceCollection AddCustomRedis(
            this IServiceCollection services,
            IConfiguration configuration) =>
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
                     configuration
                        .GetSection(nameof(ApplicationOptions.Redis))
                        .Get<RedisOptions>()
                        .ConnectionString));
#endif
        public static IServiceCollection AddCustomGraphQL(
            this IServiceCollection services,
            IConfiguration configuration) =>
            services
                .AddDataLoaderRegistry()
#if Subscriptions
                .AddInMemorySubscriptionProvider()
#endif
#if PersistedQueries
                 .AddQueryExecutor(queryExecutionBuilder => queryExecutionBuilder
                     .AddSha256DocumentHashProvider()
                     .UseActivePersistedQueryPipeline())
                 .AddRedisQueryStorage(x => x.GetRequiredService<ConnectionMultiplexer>().GetDatabase())
#endif
                .AddGraphQL(
                    serviceProvider => SchemaBuilder.New()
                        .AddServices(serviceProvider)
#if Authorization
                        .AddAuthorizeDirectiveType()
#endif
                        .ModifyOptions(
                            options =>
                            {
                                options.DefaultBindingBehavior = BindingBehavior.Explicit;

                                // Required to use custom directives
                                // https://github.com/ChilliCream/hotchocolate/issues/1470
                                options.RemoveUnreachableTypes = false;

                                options.UseXmlDocumentation = false;
                            })
                        .EnableRelaySupport()
                        .AddDirectiveType<UpperDirectiveType>()
                        .AddDirectiveType<LowerDirectiveType>()
                        .AddDirectiveType<IncludeDirectiveType>()
                        .AddDirectiveType<SkipDirectiveType>()
                        .SetSchema<MainSchema>()
                        .AddQueryType<QueryObject>()
                        .AddMutationType<MutationObject>()
#if Subscriptions
                        // .AddSubscriptionType<SubscriptionObject>()
#endif
                        .AddType<EpisodeEnumeration>()
                        .AddType<CharacterInterface>()
                        .AddType<DroidObject>()
                        .AddType<HumanObject>()
                        .AddType<HumanInputObject>()
                        .AddType(new PaginationAmountType(100))
                        .Create(),
                    configuration
                        .GetSection(nameof(ApplicationOptions.GraphQL))
                        .Get<QueryExecutionOptions>());
    }
}
