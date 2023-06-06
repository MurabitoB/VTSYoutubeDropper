using Newtonsoft.Json;

namespace KomeTube.Kernel.YtLiveChatDataModel
{
    public class Client
    {
        [JsonProperty("hl")]
        public string Hl { get; set; }

        [JsonProperty("gl")]
        public string Gl { get; set; }

        [JsonProperty("remoteHost")]
        public string RemoteHost { get; set; }

        [JsonProperty("deviceMake")]
        public string DeviceMake { get; set; }

        [JsonProperty("deviceModel")]
        public string DeviceModel { get; set; }

        [JsonProperty("visitorData")]
        public string VisitorData { get; set; }

        [JsonProperty("userAgent")]
        public string UserAgent { get; set; }

        [JsonProperty("clientName")]
        public string ClientName { get; set; }

        [JsonProperty("clientVersion")]
        public string ClientVersion { get; set; }

        [JsonProperty("osName")]
        public string OsName { get; set; }

        [JsonProperty("osVersion")]
        public string OsVersion { get; set; }

        [JsonProperty("originalUrl")]
        public string OriginalUrl { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("clientFormFactor")]
        public string ClientFormFactor { get; set; }

        [JsonProperty("browserName")]
        public string BrowserName { get; set; }

        [JsonProperty("browserVersion")]
        public string BrowserVersion { get; set; }
    }

    public class User
    {
        //public bool lockedSafetyMode { get; set; }
    }

    public class Request
    {
        [JsonProperty("useSsl")]
        public bool UseSsl { get; set; }
    }

    public class ClickTracking
    {
        [JsonProperty("clickTrackingParams")]
        public string ClickTrackingParams { get; set; }
    }

    public class INNERTUBE_CONTEXT
    {
        public INNERTUBE_CONTEXT()
        {
            Client = new Client();
            User = new User();
            Request = new Request();
            ClickTracking = new ClickTracking();
        }

        [JsonProperty("client")]
        public Client Client { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("request")]
        public Request Request { get; set; }

        [JsonProperty("clickTracking")]
        public ClickTracking ClickTracking { get; set; }
    }

    public class InnerTubeContextData
    {
        public InnerTubeContextData()
        {
            Context = new INNERTUBE_CONTEXT();
            Continuation = string.Empty;
        }

        [JsonProperty("context")]
        public INNERTUBE_CONTEXT Context { get; set; }

        [JsonProperty("continuation")]
        public string Continuation { get; set; }

        [JsonIgnore]
        [JsonProperty("INNERTUBE_API_KEY")]
        public string InnetrubeApiKey { get; set; }

        [JsonIgnore]
        [JsonProperty("ID_TOKEN")]
        public string IDToken { get; set; }

        [JsonIgnore]
        [JsonProperty("SESSION_INDEX")]
        public string SessionIndex { get; set; }

        [JsonIgnore]
        [JsonProperty("INNERTUBE_CONTEXT_CLIENT_NAME")]
        public int InnertubeContextClientName { get; set; }

        [JsonIgnore]
        [JsonProperty("INNERTUBE_CONTEXT_CLIENT_VERSION")]
        public string InnertubeContextClientVersion { get; set; }

        [JsonIgnore]
        [JsonProperty("INNERTUBE_CLIENT_VERSION")]
        public string InnertubeClientVersion { get; set; }

        [JsonIgnore]
        [JsonProperty("DATASYNC_ID")]
        public string DataSyncID { get; set; }

        [JsonIgnore]
        [JsonProperty("DELEGATED_SESSION_ID")]
        public string DelegatedSessionID { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}