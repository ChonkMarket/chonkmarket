namespace StockDataLibrary.TDAmeritrade
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Web;

    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string Scope { get; set; }
        public string TokenType { get; set; }
    }

    public class StreamerInfo
    {
        [JsonPropertyName("streamerBinaryUrl")]
        public string StreamerBinaryUrl { get; set; }

        [JsonPropertyName("streamerSocketUrl")]
        public string StreamerSocketUrl { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("tokenTimestamp")]
        public DateTime TokenTimestamp { get; set; }

        [JsonPropertyName("userGroup")]
        public string UserGroup { get; set; }

        [JsonPropertyName("accessLevel")]
        public string AccessLevel { get; set; }

        [JsonPropertyName("acl")]
        public string Acl { get; set; }

        [JsonPropertyName("appId")]
        public string AppId { get; set; }
    }

    public class Quotes
    {
        [JsonPropertyName("isNyseDelayed")]
        public bool IsNyseDelayed { get; set; }

        [JsonPropertyName("isNasdaqDelayed")]
        public bool IsNasdaqDelayed { get; set; }

        [JsonPropertyName("isOpraDelayed")]
        public bool IsOpraDelayed { get; set; }

        [JsonPropertyName("isAmexDelayed")]
        public bool IsAmexDelayed { get; set; }

        [JsonPropertyName("isCmeDelayed")]
        public bool IsCmeDelayed { get; set; }

        [JsonPropertyName("isIceDelayed")]
        public bool IsIceDelayed { get; set; }

        [JsonPropertyName("isForexDelayed")]
        public bool IsForexDelayed { get; set; }
    }

    public class KeyWrapper
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }
    }

    public class StreamerSubscriptionKeys
    {
        [JsonPropertyName("keys")]
        public List<KeyWrapper> Keys { get; set; }
    }

    public class ExchangeAgreements
    {
        [JsonPropertyName("OPRA_EXCHANGE_AGREEMENT")]
        public string OPRAEXCHANGEAGREEMENT { get; set; }

        [JsonPropertyName("NYSE_EXCHANGE_AGREEMENT")]
        public string NYSEEXCHANGEAGREEMENT { get; set; }

        [JsonPropertyName("NASDAQ_EXCHANGE_AGREEMENT")]
        public string NASDAQEXCHANGEAGREEMENT { get; set; }
    }

    public class Authorizations
    {
        [JsonPropertyName("apex")]
        public bool Apex { get; set; }

        [JsonPropertyName("levelTwoQuotes")]
        public bool LevelTwoQuotes { get; set; }

        [JsonPropertyName("stockTrading")]
        public bool StockTrading { get; set; }

        [JsonPropertyName("marginTrading")]
        public bool MarginTrading { get; set; }

        [JsonPropertyName("streamingNews")]
        public bool StreamingNews { get; set; }

        [JsonPropertyName("optionTradingLevel")]
        public string OptionTradingLevel { get; set; }

        [JsonPropertyName("streamerAccess")]
        public bool StreamerAccess { get; set; }

        [JsonPropertyName("advancedMargin")]
        public bool AdvancedMargin { get; set; }

        [JsonPropertyName("scottradeAccount")]
        public bool ScottradeAccount { get; set; }
    }

    public class Account
    {
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("accountCdDomainId")]
        public string AccountCdDomainId { get; set; }

        [JsonPropertyName("company")]
        public string Company { get; set; }

        [JsonPropertyName("segment")]
        public string Segment { get; set; }

        [JsonPropertyName("acl")]
        public string Acl { get; set; }

        [JsonPropertyName("authorizations")]
        public Authorizations Authorizations { get; set; }
    }

    public class UserPrincipal
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("userCdDomainId")]
        public string UserCdDomainId { get; set; }

        [JsonPropertyName("primaryAccountId")]
        public string PrimaryAccountId { get; set; }

        [JsonPropertyName("lastLoginTime")]
        public DateTime LastLoginTime { get; set; }

        [JsonPropertyName("tokenExpirationTime")]
        public DateTime TokenExpirationTime { get; set; }

        [JsonPropertyName("loginTime")]
        public DateTime LoginTime { get; set; }

        [JsonPropertyName("accessLevel")]
        public string AccessLevel { get; set; }

        [JsonPropertyName("stalePassword")]
        public bool StalePassword { get; set; }

        [JsonPropertyName("streamerInfo")]
        public StreamerInfo StreamerInfo { get; set; }

        [JsonPropertyName("professionalStatus")]
        public string ProfessionalStatus { get; set; }

        [JsonPropertyName("quotes")]
        public Quotes Quotes { get; set; }

        [JsonPropertyName("streamerSubscriptionKeys")]
        public StreamerSubscriptionKeys StreamerSubscriptionKeys { get; set; }

        [JsonPropertyName("exchangeAgreements")]
        public ExchangeAgreements ExchangeAgreements { get; set; }

        [JsonPropertyName("accounts")]
        public List<Account> Accounts { get; set; }
    }

    public class Credentials
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string CdDomain { get; set; }

        public string Company { get; set; }

        public string Segment { get; set; }
        public string UserGroup { get; set; }
        public string AccessLevel { get; set; }
        public string Acl { get; set; }
        public string AppId { get; set; }
        public string Authorized { get => "Y"; }
        public long Timestamp { get; set; }

        public Credentials(UserPrincipal principal)
        {
            UserId = principal.Accounts[0].AccountId;
            Token = principal.StreamerInfo.Token;
            Company = principal.Accounts[0].Company;
            Segment = principal.Accounts[0].Segment;
            CdDomain = principal.Accounts[0].AccountCdDomainId;
            UserGroup = principal.StreamerInfo.UserGroup;
            AccessLevel = principal.StreamerInfo.AccessLevel;
            AppId = principal.StreamerInfo.AppId;
            Acl = principal.StreamerInfo.Acl;
            Timestamp = new DateTimeOffset(principal.StreamerInfo.TokenTimestamp).ToUnixTimeMilliseconds();
        }

        public string ToQueryStringParameters()
        {
            return new StringBuilder()
                .Append($"userid={HttpUtility.UrlEncode(UserId)}")
                .Append($"&token={HttpUtility.UrlEncode(Token)}")
                .Append($"&company={HttpUtility.UrlEncode(Company)}")
                .Append($"&segment={HttpUtility.UrlEncode(Segment)}")
                .Append($"&cddomain={HttpUtility.UrlEncode(CdDomain)}")
                .Append($"&usergroup={HttpUtility.UrlEncode(UserGroup)}")
                .Append($"&accesslevel={HttpUtility.UrlEncode(AccessLevel)}")
                .Append($"&authorized=Y")
                .Append($"&timestamp={HttpUtility.UrlEncode(Timestamp.ToString())}")
                .Append($"&appid={HttpUtility.UrlEncode(AppId)}")
                .Append($"&acl={HttpUtility.UrlEncode(Acl)}")
                .ToString();
        }
    }

    public class Request
    {
        private static int requestId = -1;
        private string service;
        [JsonPropertyName("service")]
        public string Service {
            get => service;
            set => service = value.ToUpper();
        }
        [JsonPropertyName("requestid")]
        public int RequestId { get; }
        private string command;
        [JsonPropertyName("command")]
        public string Command {
            get => command;
            set => command = value.ToUpper();
        }
        [JsonPropertyName("account")]
        public string AcccountId { get; private set; }
        [JsonPropertyName("source")]
        public string Source { get; private set; }
        [JsonPropertyName("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new();
        public Request(UserPrincipal principal)
        {
            RequestId = requestId += 1;
            AcccountId = principal.Accounts[0].AccountId;
            Source = principal.StreamerInfo.AppId;
        }
    }

    public class RequestObject
    {
        public List<Request> requests { get; } = new List<Request>();
        public RequestObject(Request request)
        {
            requests.Add(request);
        }
    }

    public class ReceivedMessage
    {
        [JsonPropertyName("response")]
        public List<Response> Responses { get; set; }
        [JsonPropertyName("notify")]
        public List<Notify> Notifications { get; set; }
        [JsonPropertyName("snapshot")]
        public List<Snapshot> Snapshots { get; set; }
        [JsonPropertyName("data")]
        public List<DataResponse> Data { get; set; }
    }

    public class Snapshot { }

    public class DataResponse { 
        [JsonPropertyName("service")]
        public string Service { get; set; }
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        [JsonPropertyName("command")]
        public string Command { get; set; }
        [JsonPropertyName("content")]
        public List<DataContent> Content { get; set; }
    }

    public class DataContent
    {
        [JsonPropertyName("seq")]
        public int Seq { get; set; }
        [JsonPropertyName("key")]
        public string Key { get; set; }
        [JsonPropertyName("1")]
        public long TradeTime { get; set; }
        [JsonPropertyName("2")]
        public float Last { get; set; }
        [JsonPropertyName("3")]
        public float Size { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("service")]
        public string Service { get; set; }
        [JsonPropertyName("requestid")]
        public int RequestId { get; set; }
        [JsonPropertyName("command")]
        public string Command { get; set; }
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        [JsonPropertyName("content")]
        public object Content { get; set; }
    }

    public class Notify 
    {
        [JsonPropertyName("heartbeat")]
        public long Heartbeat { get; set; }
        [JsonPropertyName("service")]
        public string Service { get; set; }
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        [JsonPropertyName("content")]
        public object Content { get; set; }
    }

    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddT"));
        }
    }
}