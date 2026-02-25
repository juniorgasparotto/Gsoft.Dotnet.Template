using Shared.Infra.Module.EntityFramework.Options;

namespace Shared.Infra.Module.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// Habilita criação de tabelas para enums (SharedTypeEntity) com FK.
/// </summary>
public class EnumAsTableOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;
    private DbContextConfiguration config;

    public EnumAsTableOptionsExtension(DbContextConfiguration config)
    {
        this.config = config;
    }

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IModelCustomizer>(sp =>
        {
            var deps = sp.GetRequiredService<ModelCustomizerDependencies>();
            return new EnumAsTableModelCustomizer(deps, this.config);
        }));
    }

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using enum-as-table convention";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }
    }
}
