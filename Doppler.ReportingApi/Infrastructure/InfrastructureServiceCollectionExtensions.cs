using Doppler.ReportingApi.Authorization;
using Doppler.ReportingApi.Infrastructure;
using Doppler.ReportingApi.Services.PushContact;
using Doppler.ReportingApi.Services.SuperUserToken;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<ISummaryRepository, SummaryRepository>();
            services.AddScoped<ICampaignRepository, CampaignRepository>();
            services.AddTransient<JwtSecurityTokenHandler>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<ISuperUserTokenService, SuperUserTokenService>();
            services.AddScoped<IPushContactService, PushContactService>();
            return services;
        }
    }
}
