namespace ChonkyWeb.Controllers.v1
{
    using ChonkyWeb.Modelsl.V1ApiModels;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using StockDataLibrary;

    [Produces("application/json")]
    [ProducesErrorResponseType(typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ExternalApiController : ControllerBase
    {
        internal V1Error GenerateErrorResponse(string message, int code = -1)
        {
            return new V1Error
            {
                ErrorCode = -1,
                Message = message
            };
        }

        internal V1Response GenerateSuccessResponse(object data, string dataType)
        {
            return new V1Response
            {
                DataType = dataType,
                Data = data
            };
        }
    }

    public class ErrorResponse
    {
        public string Message {get;set;}
    }
}
