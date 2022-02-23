namespace ChonkyWeb.Controllers.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class CreateCheckoutSessionResponse
    {
        public string SessionId { get; set; }
        public string ApiKey { get; set; }
    }

    public class SubscriptionRequest
    {
        public string UserId { get; set; }
        public string CustomerId { get; set; }
    }
    public class BillingResponse
    {
        public string Url { get; set; }
        public BillingResponse(string url)
        {
            Url = url;
        }
    }

}
