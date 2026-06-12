using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Doppler.ReportingApi.Test.Controllers;
using Doppler.ReportingApi.Test.Utils;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Doppler.ReportingApi
{
    public class ExternalControllersFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            feature.Controllers.Add(typeof(HelloController).GetTypeInfo());
        }
    }

    public class AuthorizationTest
        : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly ITestOutputHelper _output;

        public AuthorizationTest(WebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            factory = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var partManager = (ApplicationPartManager)services
                        .Last(descriptor => descriptor.ServiceType == typeof(ApplicationPartManager))
                        .ImplementationInstance;

                    partManager.FeatureProviders.Add(new ExternalControllersFeatureProvider());
                });
            });
            _factory = factory;
            _output = output;
        }

        [Fact]
        public async Task GET_helloAnonymous_should_not_require_token()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            // Act
            var response = await client.GetAsync("/hello/anonymous");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GET_helloAnonymous_should_accept_any_token()
        {
            foreach (var token in AnyTokenCases())
            {
                var response = await SendAuthorizedGetAsync("/hello/anonymous", token);
                _output.WriteLine(response.GetHeadersAsString());
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GET_authenticated_endpoints_should_require_token()
        {
            foreach (var url in new[]
            {
                "/hello/valid-token",
                "/hello/superuser",
                "/accounts/123/hello",
                "/accounts/test1@test.com/hello"
            })
            {
                var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());
                var response = await client.GetAsync(url);
                _output.WriteLine(response.GetHeadersAsString());
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
            }
        }

        [Fact]
        public async Task GET_authenticated_endpoints_should_require_a_valid_token()
        {
            foreach (var testCase in InvalidTokenCases())
            {
                var response = await SendAuthorizedGetAsync(testCase.Url, testCase.Token);
                _output.WriteLine(response.GetHeadersAsString());
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.StartsWith("Bearer", response.Headers.WwwAuthenticate.ToString());
                Assert.Contains("error=\"invalid_token\"", response.Headers.WwwAuthenticate.ToString());
                Assert.Contains(testCase.ExtraErrorInfo, response.Headers.WwwAuthenticate.ToString());
            }
        }

        [Fact]
        public async Task GET_helloValidToken_should_accept_valid_token()
        {
            foreach (var token in new[]
            {
                TestJwtTokenFactory.Valid,
                TestJwtTokenFactory.ValidSuperUser,
                TestJwtTokenFactory.ValidSuperUserFalse,
                TestJwtTokenFactory.ValidAccount123Test1
            })
            {
                var response = await SendAuthorizedGetAsync("/hello/valid-token", token);
                _output.WriteLine(response.GetHeadersAsString());
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GET_helloSuperUser_should_require_a_valid_token_with_isSU_flag()
        {
            foreach (var token in new[]
            {
                TestJwtTokenFactory.Valid,
                TestJwtTokenFactory.ValidSuperUserFalse,
                TestJwtTokenFactory.ValidAccount123Test1
            })
            {
                var response = await SendAuthorizedGetAsync("/hello/superuser", token);
                _output.WriteLine(response.GetHeadersAsString());
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }

        [Fact]
        public async Task GET_helloSuperUser_should_accept_valid_token_with_isSU_flag()
        {
            var response = await SendAuthorizedGetAsync("/hello/superuser", TestJwtTokenFactory.ValidSuperUser);
            _output.WriteLine(response.GetHeadersAsString());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GET_account_endpoint_should_require_a_valid_token_with_isSU_flag_or_a_token_for_the_right_account()
        {
            foreach (var testCase in new[]
            {
                (Url: "/accounts/123/hello", Token: TestJwtTokenFactory.Valid),
                (Url: "/accounts/123/hello", Token: TestJwtTokenFactory.ValidSuperUserFalse),
                (Url: "/accounts/456/hello", Token: TestJwtTokenFactory.ValidAccount123Test1),
                (Url: "/accounts/test1@test.com/hello", Token: TestJwtTokenFactory.Valid),
                (Url: "/accounts/test1@test.com/hello", Token: TestJwtTokenFactory.ValidSuperUserFalse),
                (Url: "/accounts/test2@test.com/hello", Token: TestJwtTokenFactory.ValidAccount123Test1)
            })
            {
                var response = await SendAuthorizedGetAsync(testCase.Url, testCase.Token);
                _output.WriteLine(response.GetHeadersAsString());
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }

        [Fact]
        public async Task GET_account_endpoint_should_accept_valid_token_with_isSU_flag_or_a_token_for_the_right_account()
        {
            foreach (var testCase in new[]
            {
                (Url: "/accounts/123/hello", Token: TestJwtTokenFactory.ValidSuperUser),
                (Url: "/accounts/123/hello", Token: TestJwtTokenFactory.ValidAccount123Test1),
                (Url: "/accounts/test1@test.com/hello", Token: TestJwtTokenFactory.ValidSuperUser),
                (Url: "/accounts/test1@test.com/hello", Token: TestJwtTokenFactory.ValidAccount123Test1)
            })
            {
                var response = await SendAuthorizedGetAsync(testCase.Url, testCase.Token);
                _output.WriteLine(response.GetHeadersAsString());
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private async Task<HttpResponseMessage> SendAuthorizedGetAsync(string url, string token)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());
            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            return await client.SendAsync(request);
        }

        private static IEnumerable<string> AnyTokenCases()
        {
            yield return TestJwtTokenFactory.NoExpiration;
            yield return TestJwtTokenFactory.NoExpirationSuperUser;
            yield return TestJwtTokenFactory.NoExpirationSuperUserFalse;
            yield return TestJwtTokenFactory.NoExpirationAccount123Test1;
            yield return TestJwtTokenFactory.Valid;
            yield return TestJwtTokenFactory.Expired;
            yield return TestJwtTokenFactory.ValidSuperUser;
            yield return TestJwtTokenFactory.ExpiredSuperUser;
            yield return TestJwtTokenFactory.ValidSuperUserFalse;
            yield return TestJwtTokenFactory.ValidAccount123Test1;
            yield return TestJwtTokenFactory.ExpiredAccount123Test1;
            yield return TestJwtTokenFactory.Broken;
        }

        private static IEnumerable<(string Url, string Token, string ExtraErrorInfo)> InvalidTokenCases()
        {
            var noExpirationTokens = new[]
            {
                TestJwtTokenFactory.NoExpiration,
                TestJwtTokenFactory.NoExpirationSuperUser,
                TestJwtTokenFactory.NoExpirationAccount123Test1
            };

            var expiredTokens = new[]
            {
                TestJwtTokenFactory.Expired,
                TestJwtTokenFactory.ExpiredSuperUser,
                TestJwtTokenFactory.ExpiredAccount123Test1
            };

            foreach (var url in new[]
            {
                "/hello/valid-token",
                "/hello/superuser",
                "/accounts/123/hello",
                "/accounts/test1@test.com/hello"
            })
            {
                foreach (var token in noExpirationTokens)
                {
                    yield return (url, token, "error_description=\"The token has no expiration\"");
                }

                foreach (var token in expiredTokens)
                {
                    yield return (url, token, "error_description=\"The token expired at");
                }

                yield return (url, TestJwtTokenFactory.Broken, string.Empty);
            }
        }
    }
}
