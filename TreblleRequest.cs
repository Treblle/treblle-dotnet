using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Treblle
{
    public class TreblleCall
    {
        [JsonProperty("api_key")]
        public string ApiKey { get; set; }
        [JsonProperty("project_id")]
        public string ProjectId { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("sdk")]
        public string Sdk { get; set; }
        [JsonProperty("data")]
        public TreblleData TreblleData { get; set; }

        public TreblleCall()
        {
            Version = "0.1";
            Sdk = ".NET";

            TreblleData = new TreblleData();
        }
    }

    public class TreblleData
    {
        [JsonProperty("server")]
        public Server Server { get; set; }
        [JsonProperty("php")]
        public PHP PHP { get; set; }
        [JsonProperty("request")]
        public TreblleRequest TreblleRequest { get; set; }
        [JsonProperty("response")]
        public TreblleResponse TreblleResponse { get; set; }
        [JsonProperty("errors")]
        public JArray Errors { get; set; }

        public TreblleData()
        {
            Server = new Server();
            PHP = new PHP();
            TreblleRequest = new TreblleRequest();
            TreblleResponse = new TreblleResponse();
            Errors = new JArray();
        }

        [JsonProperty("git")]
        public string Git { get; set; }
        [JsonProperty("meta")]
        public string Meta { get; set; }
    }

    public class TreblleRequest
    {
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
        [JsonProperty("ip")]
        public string IP { get; set; }
        [JsonProperty("url")]
        public string URL { get; set; }
        [JsonProperty("user_agent")]
        public string UserAgent { get; set; }
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("headers")]
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        [JsonProperty("body")]
        public JToken Body { get; set; }
        [JsonProperty("raw")]
        public string Raw { get; set; }
    }

    public class Server
    {
        [JsonProperty("timezone")]
        private string Timezone { get; set; }
        [JsonProperty("os")]
        public OS OS { get; set; }
        [JsonProperty("software")]
        private string Software { get; set; }
        [JsonProperty("signature")]
        public string Signature { get; set; }
        [JsonProperty("protocol")]
        public string Protocol { get; set; }
        [JsonProperty("encoding")]
        public string Encoding { get; set; }

        public Server()
        {
            Timezone = TimeZone.CurrentTimeZone.StandardName;
            Software = RuntimeInformation.FrameworkDescription.ToString();
            OS = new OS();
        }
    }

    public class PHP
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("sapi")]
        public string SApi { get; set; }
        [JsonProperty("expose_php")]
        public string ExposePHP { get; set; }
        [JsonProperty("display_errors")]
        public string DisplayErrors { get; set; }

        public PHP()
        {
            Version = "";
            SApi = "";
            ExposePHP = "";
            DisplayErrors = "";
        }
    }

    public class OS
    {
        [JsonProperty("name")]
        private string Name = RuntimeInformation.OSDescription;
        [JsonProperty("release")]
        private string Release = Environment.OSVersion.ToString();
        [JsonProperty("architecture")]
        private string Architecture = RuntimeInformation.OSArchitecture.ToString();
    }

    public class TreblleResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("headers")]
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        [JsonProperty("load_time")]
        public int Loadtime { get; set; }
        [JsonProperty("size")]
        public int Size { get; set; }
        [JsonProperty("body")]
        public JToken Body { get; set; }
    }
}