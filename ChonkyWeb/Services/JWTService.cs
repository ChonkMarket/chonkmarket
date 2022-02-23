namespace ChonkyWeb.Services
{
    using ChonkyWeb.Models;
    using Microsoft.IdentityModel.Tokens;
    using StockDataLibrary;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;

    public class JWTService
    {
        public static string GenerateJWT(Account user, string secret, Scope scope)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);
            // TODO: use refresh tokens in the frontend instead
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object> { { "scope", scope.ToString() } },
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
