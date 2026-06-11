using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;

namespace Doppler.ReportingApi.Authorization
{
    public static class TokenServiceCollectionExtensions
    {
        public static IServiceCollection AddJwtToken([NotNull] this IServiceCollection services)
        {
            return services.AddSingleton(provider =>
            {
                var jwtOptions = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
                var webHostEnvironment = provider.GetRequiredService<IWebHostEnvironment>();
                var fileInfo = webHostEnvironment.ContentRootFileProvider.GetFileInfo(jwtOptions.RsaParametersFilePath);

                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException(
                        $"JWT private key file was not found: {jwtOptions.RsaParametersFilePath}");
                }

                using var stream = fileInfo.CreateReadStream();
                using var reader = new StreamReader(stream);
                using var publicAndPrivate = new RSACryptoServiceProvider();

                publicAndPrivate.FromXmlString(reader.ReadToEnd());

                var key = new RsaSecurityKey(publicAndPrivate.ExportParameters(true));
                return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
            });
        }
    }
}
