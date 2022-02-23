namespace ChonkyWebTests.Authorization
{
    using ChonkyWeb;
    using ChonkyWeb.Services;
    using StockDataLibrary;
    using System.Threading.Tasks;
    using Xunit;

    public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        public AuthorizationTests(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetHost()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/get_token");

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetIdentity()
        {
            var client = _factory.CreateClient();
            var jwt = JWTService.GenerateJWT(_factory.User, _factory.ChonkyConfiguration.Secret, Scope.frontend);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.GetAsync("/api/identity");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetIdentityFailsWithApiToken()
        {
            var client = _factory.CreateClient();
            var jwt = JWTService.GenerateJWT(_factory.User, _factory.ChonkyConfiguration.Secret, Scope.api);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.GetAsync("/api/identity");

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifiesValidApiToken()
        {
            var client = _factory.CreateClient();
            var jwt = JWTService.GenerateJWT(_factory.User, _factory.ChonkyConfiguration.Secret, Scope.api);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.GetAsync("/api/v1/verify");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }
}
