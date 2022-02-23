namespace ChonkyWeb.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Role Role { get; set; } = Role.User;
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? Updated { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public string StripeCustomerId { get; set; }
        public string DiscordAccessToken { get; set; }
        public long DiscordUserId { get; set; }
        public string DiscordNameIdentifier { get; set; }
        public string AvatarUrl { get; set; }

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }
    }

    public class AccountApiResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Role Role { get; set; } = Role.User;
        public string DiscordNameIdentifier { get; set; }
        public string AvatarUrl { get; set; }
    }
}
