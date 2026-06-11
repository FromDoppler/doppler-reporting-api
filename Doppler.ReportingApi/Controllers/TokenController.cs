using System.Collections.Generic;
using Doppler.ReportingApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doppler.ReportingApi.Controllers
{
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public TokenController(IJwtTokenGenerator jwtTokenGenerator)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("debug/token")]
        public IActionResult GenerateDebugToken()
        {
            var payload = new Dictionary<string, object>
            {
                { "isSU", true }
            };

            var token = _jwtTokenGenerator.GenerateToken(payload);

            return Ok(new
            {
                token
            });
        }
    }
}
