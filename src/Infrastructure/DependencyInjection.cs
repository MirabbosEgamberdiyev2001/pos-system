using Microsoft.Extensions.DependencyInjection;
using POS.Application.Interfaces;
using POS.Infrastructure.Security;

namespace POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddTransient<IPasswordHasher, BcryptPasswordHasher>();
        return services;
    }
}
