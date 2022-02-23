namespace ChonkyWeb.Controllers
{
    using Azure.Messaging.ServiceBus;
    using Azure.Storage.Queues;
    using ChonkyWeb.Models;
    using ChonkyWeb.Modelsl.V1ApiModels;
    using ChonkyWeb.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using StockDataLibrary;
    using StockDataLibrary.Services;
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class LoginController : Controller
    {
        private readonly AccountDbContext _dbContext;
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private readonly QueueClient _userUpdateQueueClient;

        public LoginController(AccountDbContext dbContext, ChonkyConfiguration chonkyConfiguration)
        {
            _dbContext = dbContext;
            _chonkyConfiguration = chonkyConfiguration;
            _userUpdateQueueClient = new QueueClient(chonkyConfiguration.AzureBlobKeyConnectionString, chonkyConfiguration.UserUpdateQueueName);
            _userUpdateQueueClient.CreateIfNotExists();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "Discord");
        }

        [HttpDelete("/session")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok();
        }

        [HttpGet("/get_token")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetToken()
        {
            return await GenerateTokenAsync(Scope.frontend);
        }


        [HttpGet("/get_api_token")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetApiToken()
        {
            return await GenerateTokenAsync(Scope.api);
        }

        private async Task<IActionResult> GenerateTokenAsync(Scope scope)
        {
            if (User.Identity.AuthenticationType == "Discord")
            {
                var discordId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var account = _dbContext.Accounts.SingleOrDefault(user => user.DiscordUserId == discordId);
                if (account == null)
                {
                    account = new Account
                    {
                        Email = User.FindFirst(ClaimTypes.Email)?.Value,
                        AvatarUrl = User.FindFirst("urn:discord:avatar:url")?.Value,
                        Name = User.FindFirst(ClaimTypes.Name)?.Value,
                        DiscordUserId = discordId,
                        DiscordNameIdentifier = User.FindFirst("urn:discord:user:discriminator")?.Value
                    };
                    _dbContext.Accounts.Add(account);
                    await _dbContext.SaveChangesAsync();
                    // don't await this, if it fails or w/e who cares
                    //
                    _ = PushUpdatedUserRecord(account);
                }
                var response = new V1Response()
                {
                    DataType = "token",
                    Data = JWTService.GenerateJWT(account, _chonkyConfiguration.Secret, scope)
                };
                return Ok(response);
            } else
            {
                return BadRequest();
            }
        }
        
        private async Task PushUpdatedUserRecord(Account account)
        {
            var response = new V1Response<Account>
            {
                Data = account
            };
            await _userUpdateQueueClient.SendMessageAsync(JsonSerializer.Serialize(response));
        }
    }
}
