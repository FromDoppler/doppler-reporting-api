using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Doppler.ReportingApi.Test.Utils
{
    public static class TestJwtTokenFactory
    {
        public static string Broken => "not-a-valid-jwt";

        public static string NoExpiration => BuildToken();

        public static string NoExpirationSuperUser => BuildToken(isSuperUser: true);

        public static string NoExpirationSuperUserFalse => BuildToken(isSuperUser: false);

        public static string NoExpirationAccount123Test1 => BuildToken(accountId: "123", accountName: "test1@test.com");

        public static string Valid => BuildToken(expires: new DateTime(2033, 5, 18, 0, 0, 0, DateTimeKind.Utc));

        public static string Expired => BuildToken(expires: new DateTime(2001, 9, 8, 0, 0, 0, DateTimeKind.Utc));

        public static string ValidSuperUser => BuildToken(isSuperUser: true, expires: new DateTime(2033, 5, 18, 0, 0, 0, DateTimeKind.Utc));

        public static string ExpiredSuperUser => BuildToken(isSuperUser: true, expires: new DateTime(2001, 9, 8, 0, 0, 0, DateTimeKind.Utc));

        public static string ValidSuperUserFalse => BuildToken(isSuperUser: false, expires: new DateTime(2033, 5, 18, 0, 0, 0, DateTimeKind.Utc));

        public static string ValidAccount123Test1 => BuildToken(accountId: "123", accountName: "test1@test.com", expires: new DateTime(2033, 5, 18, 0, 0, 0, DateTimeKind.Utc));

        public static string ExpiredAccount123Test1 => BuildToken(accountId: "123", accountName: "test1@test.com", expires: new DateTime(2001, 9, 8, 0, 0, 0, DateTimeKind.Utc));

        private static string BuildToken(
            bool? isSuperUser = null,
            string accountId = null,
            string accountName = null,
            DateTime? expires = null)
        {
            var claims = new List<Claim>();

            if (isSuperUser.HasValue)
            {
                claims.Add(new Claim("isSU", isSuperUser.Value.ToString().ToLowerInvariant()));
            }

            if (accountId != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, accountId));
            }

            if (accountName != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, accountName));
                claims.Add(new Claim(ClaimTypes.Role, "USER"));
            }

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expires,
                signingCredentials: new SigningCredentials(new RsaSecurityKey(GetPrivateKey()), SecurityAlgorithms.RsaSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static RSAParameters GetPrivateKey()
        {
            var keyPath = Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Doppler.ReportingApi",
                "Resources",
                "Jwt",
                "key.dev.xml");

            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(File.ReadAllText(keyPath));
            return rsa.ExportParameters(true);
        }
    }
}
