namespace ChonkyWeb.Middleware
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Text;
    using StockDataLibrary;
    using ChonkyWeb.Models;

    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ChonkyConfiguration _chonkyConfiguration;

        public JwtMiddleware(RequestDelegate next, ChonkyConfiguration chonkyConfiguration)
        {
            _next = next;
            _chonkyConfiguration = chonkyConfiguration;
        }

        public async Task Invoke(HttpContext context, AccountDbContext dbContext)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                await AttachAccountToContext(context, dbContext, token);

            await _next(context);
        }

        private async Task AttachAccountToContext(HttpContext context, AccountDbContext dbContext, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_chonkyConfiguration.Secret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var accountId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                // attach account to context on successful jwt validation
                context.Items["Account"] = await dbContext.Accounts.FindAsync(accountId);
            }
            catch
            {
                // do nothing if jwt validation fails
                // account is not attached to context so request won't have access to secure routes
            }
        }
    }
}
