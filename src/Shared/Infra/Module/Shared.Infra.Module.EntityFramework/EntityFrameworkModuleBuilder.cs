namespace Shared.Infra.Module.EntityFramework;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Core.Repositories;
using Shared.Infra.Module.EntityFramework.Conventions;
using Shared.Infra.Module.EntityFramework.Logging;
using Shared.Infra.Module.EntityFramework.Options;
using Shared.Infra.Module.EntityFramework.Repositories;

public enum ProviderType
{
    Sqlite,
    Postgres,
    SqlServer
}

/// <summary>
/// Fluent builder for configuring Entity Framework Module with strongly-typed DbContext registrations.
/// </summary>
public class EntityFrameworkModuleBuilder
{
    public List<Action<IServiceCollection>> AddActions { get; } = [];
    public List<Type> RegisteredDbContextTypes { get; } = [];

    /// <summary>
    /// Adds a DbContext type and registers IRepository&lt;T&gt; for each entity exposed as DbSet&lt;T&gt;.
    /// </summary>
    public EntityFrameworkModuleBuilder AddDbContext<TContext>(ProviderType? providerType = null) where TContext : DbContext
    {
        var contextType = typeof(TContext);
        this.RegisteredDbContextTypes.Add(contextType);

        var entityTypes = GetEntityTypesFromDbSets(contextType);
        var repositoryType = typeof(Repository<,>);

        AddActions.Add((services) =>
        {
            services.AddDbContext<TContext>((provider, options) =>
            {
                var appSettings = provider.GetRequiredService<IOptions<EntityFrameworkModuleOptions>>();

                if (appSettings?.Value?.DbContexts == null)
                    return;

                var config = appSettings.Value.DbContexts.GetConfigForContext(contextType.Name!);

                if (config != null)
                {
                    ConfigureDbContextOptionsStatic(config, options, providerType);
                }
            });

            // Adiciona os Repositories para cada entidade do DbContext Repository<DbContext, Entity>
            foreach (var entityType in entityTypes)
            {
                var implType = repositoryType.MakeGenericType(contextType, entityType);
                var serviceType = typeof(IRepository<>).MakeGenericType(entityType);
                services.AddTransient(serviceType, implType);
            }
        });

        return this;
    }

    private static IEnumerable<Type> GetEntityTypesFromDbSets(Type dbContextType)
    {
        var dbSetDef = typeof(DbSet<>);
        return dbContextType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == dbSetDef)
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Where(t => t.IsClass);
    }

    /// <summary>
    /// Static version of ConfigureDbContextOptions for use in the generic method.
    /// </summary>
    private static void ConfigureDbContextOptionsStatic(
        DbContextConfiguration config,
        DbContextOptionsBuilder optionsBuilder,
        ProviderType? providerType = null
    )
    {
        var providerName = providerType != null ? Enum.GetName(typeof(ProviderType), providerType) : config.DefaultProvider;
        if (string.IsNullOrEmpty(providerName) || !config.Providers!.ContainsKey(providerName))
            return;
        var defaultProviderOptions = config.Providers[providerName];

        // Common configurations
        if (defaultProviderOptions.EnableSensitiveDataLogging == true)
        {
            // Faz o EF incluir valores reais dos par�metros nos logs e exceptions.
            // EX: Executing DbCommand (1ms) [Parameters=[@__id_0='123']]
            optionsBuilder.EnableSensitiveDataLogging();
        }

        if (defaultProviderOptions.EnableDetailedErrors == true)
        {
            // Faz o EF validar mais coisas em runtime e dar exce��es mais claras quando algo
            // d� errado (principalmente materializa��o de dados).
            optionsBuilder.EnableDetailedErrors();
        }

        if (config.EnableLogging)
        {
            //optionsBuilder
            //    .LogTo(Console.WriteLine, LogLevel.Information)
            //    .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning));

            optionsBuilder.AddInterceptors([new QueriesToFileInterceptor()]);
        }

        // Configurar provider espec�fico
        switch (providerName?.ToLowerInvariant())
        {
            case "sqlite":
                ConfigureSqliteProviderStatic(optionsBuilder, defaultProviderOptions);
                break;
            case "postgres":
            case "postgresql":
            case "npgsql":
                ConfigurePostgresProviderStatic(optionsBuilder, defaultProviderOptions);
                break;
            case "sqlserver":
            case "mssql":
                ConfigureSqlServerProviderStatic(optionsBuilder, defaultProviderOptions);
                break;
            default:
                ConfigureSqliteProviderStatic(optionsBuilder, defaultProviderOptions);
                break;
        }

        // Configure retry policy if enabled
        if (defaultProviderOptions.EnableRetryOnFailure == true)
        {
            optionsBuilder.EnableServiceProviderCaching();
        }

        // Enum-as-table: SharedTypeEntity<Dictionary>
        var optionsBuilderInfrastructure = ((IDbContextOptionsBuilderInfrastructure)optionsBuilder);
        optionsBuilderInfrastructure.AddOrUpdateExtension(new EnumAsTableOptionsExtension(config));
    }

    private static void ConfigureSqliteProviderStatic(DbContextOptionsBuilder optionsBuilder, DatabaseProviderOptions providerOptions)
    {
        optionsBuilder.UseSqlite(providerOptions.ConnectionString!, sqliteOptions =>
        {
            if (!string.IsNullOrEmpty(providerOptions.MigrationsAssembly))
            {
                sqliteOptions.MigrationsAssembly(providerOptions.MigrationsAssembly);
            }

            if (!string.IsNullOrEmpty(providerOptions.MigrationsHistoryTable))
            {
                sqliteOptions.MigrationsHistoryTable(providerOptions.MigrationsHistoryTable);
            }

            if (providerOptions.CommandTimeout.HasValue)
            {
                sqliteOptions.CommandTimeout(providerOptions.CommandTimeout.Value);
            }
        });
    }

    private static void ConfigurePostgresProviderStatic(DbContextOptionsBuilder optionsBuilder, DatabaseProviderOptions providerOptions)
    {
        optionsBuilder.UseNpgsql(providerOptions.ConnectionString!, npgsqlOptions =>
        {
            if (!string.IsNullOrEmpty(providerOptions.MigrationsAssembly))
            {
                npgsqlOptions.MigrationsAssembly(providerOptions.MigrationsAssembly);
            }

            if (!string.IsNullOrEmpty(providerOptions.MigrationsHistoryTable))
            {
                npgsqlOptions.MigrationsHistoryTable(providerOptions.MigrationsHistoryTable);
            }

            if (providerOptions.CommandTimeout.HasValue)
            {
                npgsqlOptions.CommandTimeout(providerOptions.CommandTimeout.Value);
            }

            if (providerOptions.EnableRetryOnFailure == true)
            {
                var maxRetryCount = providerOptions.MaxRetryCount ?? 3;
                var maxRetryDelay = TimeSpan.FromSeconds(providerOptions.MaxRetryDelaySeconds ?? 30);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount, maxRetryDelay, null);
            }
        });
    }

    private static void ConfigureSqlServerProviderStatic(DbContextOptionsBuilder optionsBuilder, DatabaseProviderOptions providerOptions)
    {
        optionsBuilder.UseSqlServer(providerOptions.ConnectionString!, sqlServerOptions =>
        {
            if (!string.IsNullOrEmpty(providerOptions.MigrationsAssembly))
            {
                sqlServerOptions.MigrationsAssembly(providerOptions.MigrationsAssembly);
            }

            if (!string.IsNullOrEmpty(providerOptions.MigrationsHistoryTable))
            {
                sqlServerOptions.MigrationsHistoryTable(providerOptions.MigrationsHistoryTable);
            }

            if (providerOptions.CommandTimeout.HasValue)
            {
                sqlServerOptions.CommandTimeout(providerOptions.CommandTimeout.Value);
            }

            if (providerOptions.EnableRetryOnFailure == true)
            {
                var maxRetryCount = providerOptions.MaxRetryCount ?? 3;
                var maxRetryDelay = TimeSpan.FromSeconds(providerOptions.MaxRetryDelaySeconds ?? 30);
                sqlServerOptions.EnableRetryOnFailure(maxRetryCount, maxRetryDelay, null);
            }
        });
    }

}
