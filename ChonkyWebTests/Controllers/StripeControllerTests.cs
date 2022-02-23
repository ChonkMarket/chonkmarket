namespace ChonkyWebTests
{
    using ChonkyWeb;
    using ChonkyWeb.Models;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class StripeControllerTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;

        public StripeControllerTests(CustomWebApplicationFactory<Startup> factory) {
            _factory = factory;
        }

        [Fact]
        public async Task TestSubscriptionDeleteWebhook()
        {
            var fixture = File.ReadAllText(Path.Join("Fixtures", "SubscriptionDeleted.json"));
            var client = _factory.CreateClient();
            var jsonContent = new StringContent(fixture, Encoding.UTF8, "application/json");
            var user = _factory.User;
            var dbContext = _factory.GenerateDbContext();

            Assert.Equal(Role.Subscriber, user.Role);
            var response = await client.PostAsync("/api/stripe/webhooks", jsonContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            user = dbContext.Accounts.Find(user.Id);
            Assert.Equal(Role.User, user.Role);
        }


        [Fact]
        public async Task TestSubscriptionCreateWebhook()
        {
            var client = _factory.CreateClient();

            var dbContext = _factory.GenerateDbContext();
            var user = new Account { StripeCustomerId = "cus_JGHvLS4Z5AGbuZ", Role = Role.User };
            Assert.Equal(Role.User, user.Role);
            dbContext.Accounts.Add(user);
            dbContext.SaveChanges();

            var fixture = File.ReadAllText(Path.Join("Fixtures", "SubscriptionCreated.json"));
            var jsonContent = new StringContent(fixture, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/stripe/webhooks", jsonContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            dbContext.Entry(user).Reload();
            Assert.Equal(Role.Subscriber, user.Role);
        }
    }
}
