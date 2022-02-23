namespace ChonkyWeb.Controllers.v1
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using StockDataLibrary;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Route("api/v1/[controller]")]
    [ApiController]
    public class VerifyController : ExternalApiController
    {
        /// <summary>
        /// Verifies that you have a working API token
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200)]
        [ScopeAuthorize(Scope.api)]
        [HttpGet]
        public IActionResult Verify()
        {
            return Ok();
        }

    }
}
