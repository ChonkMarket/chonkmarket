namespace ChonkyWeb.Controllers
{
    using AutoMapper;
    using ChonkyWeb.Models;
    using ChonkyWeb.Modelsl.V1ApiModels;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [ApiController]
    public class IdentityController : BaseController
    {
        private readonly IMapper _mapper;
        public IdentityController(IMapper mapper)
        {
            _mapper = mapper;
        }

        [HttpGet]
        [ScopeAuthorize(StockDataLibrary.Scope.frontend)]
        public IActionResult Get()
        {
            return Ok(new V1Response()
            {
                Success = true,
                Data = _mapper.Map<AccountApiResponse>(Account)
            });
        }
    }
}
