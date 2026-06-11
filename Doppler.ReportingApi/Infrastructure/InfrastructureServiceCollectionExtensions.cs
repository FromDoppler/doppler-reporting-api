using Doppler.ReportingApi.Authorization;
using Doppler.ReportingApi.Infrastructure;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
            services.AddScoped<ISummaryRepository, SummaryRepository>();
            services.AddScoped<ICampaignRepository, CampaignRepository>();
            services.AddTransient<JwtSecurityTokenHandler>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            return services;
        }
    }
}
