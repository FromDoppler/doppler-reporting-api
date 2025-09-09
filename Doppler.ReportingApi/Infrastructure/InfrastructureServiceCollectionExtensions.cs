using Doppler.ReportingApi.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
            services.AddScoped<ISummaryRepository, SummaryRepository>();
            services.AddScoped<ICampaignRepository, CampaignRepository>();
            return services;
        }
    }
}
