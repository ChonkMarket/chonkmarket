namespace ChonkyWeb.Controllers
{
    using Azure.Messaging.ServiceBus;
    using ChonkyWeb.Controllers.Models;
    using ChonkyWeb.Models;
    using ChonkyWeb.Modelsl.V1ApiModels;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Services;
    using Stripe;
    using Stripe.Checkout;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : BaseController
    {
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private readonly ILogger<StripeController> _logger;
        private readonly IServiceBusProvider _serviceBusProvider;
        private readonly AccountDbContext _dbContext;

        public StripeController(ChonkyConfiguration chonkyConfiguration, ILogger<StripeController> logger,
            IServiceBusProvider serviceBusProvider, AccountDbContext dbContext)
        {
            _chonkyConfiguration = chonkyConfiguration;
            _logger = logger;
            _serviceBusProvider = serviceBusProvider;
            _dbContext = dbContext;
            StripeConfiguration.ApiKey = _chonkyConfiguration.StripeSecretKey;
        }

        [ScopeAuthorize(Scope.frontend)]
        [HttpPost("billing")]
        public IActionResult Billing()
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = Account.StripeCustomerId,
                ReturnUrl = $"https://{_chonkyConfiguration.WebHostName}/user/subscription"
            };
            var service = new Stripe.BillingPortal.SessionService();
            var session = service.Create(options);

            return Ok(new V1Response<BillingResponse>() { Data = new BillingResponse(session.Url) });
        }

        [ScopeAuthorize(Scope.frontend)]
        [HttpGet("success")]
        public async Task<ActionResult> Success([FromQuery] string sessionId)
        {
            using (_logger.BeginScope($"Subscription success, session ID: {sessionId}"))
            {
                try
                {
                    var service = new SessionService();
                    var session = await service.GetAsync(sessionId);

                    var customerId = session.CustomerId;
                    _logger.LogInformation($"Stripe Customer ID: {customerId}");
                    _logger.LogInformation($"Account ID: {Account.Id}");

                    Account.Role = Role.Subscriber;
                    Account.StripeCustomerId = customerId;
                    _dbContext.Update(Account);
                    var dbUpdateTask = _dbContext.SaveChangesAsync();

                    var subscriptionRequest = new SubscriptionRequest { UserId = Account.Id.ToString(), CustomerId = customerId };
                    var serviceBusSenderTask = (await _serviceBusProvider.GetServiceBusSender(_chonkyConfiguration.ServiceBusSubscriptionsTopic))
                        .SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(subscriptionRequest)));

                    await Task.WhenAll(new Task[] { dbUpdateTask, serviceBusSenderTask });
                    _logger.LogInformation("Subscription created");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to update Auth0");
                    _logger.LogError(e.Message);
                    _logger.LogError(e.InnerException?.Message);
                    throw;
                }
            }
            return Ok(new V1Response());
        }

        [ScopeAuthorize(Scope.frontend)]
        [HttpGet("create-checkout-session")]
        public async Task<ActionResult> CreateCheckoutSession()
        {
            SessionCreateOptions options = new()
            {
                // See https://stripe.com/docs/api/checkout/sessions/create
                // for additional parameters to pass.
                // {CHECKOUT_SESSION_ID} is a string literal; do not change it!
                // the actual Session ID is returned in the query parameter when your customer
                // is redirected to the success page.
                SuccessUrl = $"https://{_chonkyConfiguration.WebServerHostName}/checkoutsuccess?sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"https://{_chonkyConfiguration.WebServerHostName}/",
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = _chonkyConfiguration.StripeSubscriptionPriceId,
                        Quantity = 1,
                    },
                },
            };

            var stripeCustomerId = Account.StripeCustomerId;
            if (string.IsNullOrEmpty(stripeCustomerId))
            {
                options.CustomerEmail = Account.Email;
            } else
            {
                options.Customer = stripeCustomerId;
            }
            try
            {
                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return Ok(JsonSerializer.Serialize(new CreateCheckoutSessionResponse { SessionId = session.Id, ApiKey = _chonkyConfiguration.StripePublicKey }));
            }
            catch (StripeException e)
            {
                _logger.LogError(e.StripeError.Message);

                return BadRequest(e.StripeError.Message);
            }
        }

        [HttpPost("webhooks")]
        public async Task<IActionResult> Webhooks()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);

                // Handle the event
                if (stripeEvent.Type == Events.CustomerSubscriptionDeleted)
                {
                    using (_logger.BeginScope($"Subscription cancelled"))
                    {
                        var subscription = stripeEvent.Data.Object as Subscription;
                        _logger.LogInformation($"Customer ID: {subscription.CustomerId} Event ID: {subscription.Id}");
                        var account = _dbContext.Accounts.SingleOrDefault(c => c.StripeCustomerId == subscription.CustomerId);
                        if (account == null)
                        {
                            _logger.LogError($"Failed to look up user for subscription {subscription.Id}");
                            return BadRequest();
                        }
                        _logger.LogInformation($"Removing Premium role from User {account.Id} - {account.Name}");
                        account.Role = Role.User;
                        _dbContext.Update(account);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionCreated)
                {
                    using (_logger.BeginScope($"Subscription created"))
                    {
                        var subscription = stripeEvent.Data.Object as Subscription;
                        _logger.LogInformation($"Customer ID: {subscription.CustomerId} Event ID: {subscription.Id}");
                        var account = _dbContext.Accounts.SingleOrDefault(c => c.StripeCustomerId == subscription.CustomerId);
                        if (account == null)
                        {
                            _logger.LogError($"Failed to look up user for subscription {subscription.Id}");
                            return BadRequest();
                        }
                        _logger.LogInformation($"Adding premium role to User {account.Id} - {account.Name}");
                        account.Role = Role.Subscriber;
                        _dbContext.Update(account);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    _logger.LogInformation("Unhandled event type: {0}", stripeEvent.Type);
                }
                return Ok();
            }
            catch (StripeException)
            {
                return BadRequest();
            }
        }
    }
}
