namespace ChonkyWeb
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Options;
    using StockDataLibrary;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    internal class ScopeAuthorizeAttribute : AuthorizeAttribute
    {
        const string POLICY_PREFIX = "Scope";
        public string Scope
        {
            get
            {
                return Policy[POLICY_PREFIX.Length..];
            }
            set
            {
                Policy = $"{POLICY_PREFIX}{value}";
            }
        }

        public ScopeAuthorizeAttribute(Scope scope) => Scope = scope.ToString();
    }



    internal class ScopeAuthorizationProvider : IAuthorizationPolicyProvider
    {
        const string POLICY_PREFIX = "Scope";

        public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public ScopeAuthorizationProvider(IOptions<AuthorizationOptions> options)
        {
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();
        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                string scope = policyName[POLICY_PREFIX.Length..];
                var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme);
                policy.AddRequirements(new ScopeRequirement(Enum.Parse<Scope>(scope)));
                return Task.FromResult(policy.Build());
            }

            return Task.FromResult<AuthorizationPolicy>(null);
        }
    }

    internal class ScopeRequirement : IAuthorizationRequirement
    {
        public string Scope { get; private set; }
        public ScopeRequirement(Scope scope)
        {
            Scope = scope.ToString();
        }
    }

    internal class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
        {
            {
                if (!context.User.HasClaim(c => c.Type == "scope"))
                {
                    return Task.CompletedTask;
                }

                var scopes = context.User.FindFirst(c => c.Type == "scope").Value.Split(" ");

                if (scopes.Contains(requirement.Scope))
                {
                    context.Succeed(requirement);
                }
                return Task.CompletedTask;
            }
        }
    }
}
