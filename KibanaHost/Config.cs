namespace KibanaHost
{
    class Config
    {
        public static Config Instance = new Config();

        public Config()
        {
            Port = 5602;
            Host = "0.0.0.0";
            Elasticsearch = "http://localhost:9200";
            KibanaIndex = ".kibana";
            DefaultAppId = "discover";
            RequestTimeout = 60;
            ShardTimeout = 30000;
            VerifySsl = true;
        }

        public int Port { get; set; }
        public string Host { get; set; }
        public string Elasticsearch { get; set; }
        public string KibanaIndex { get; set; }
        public string DefaultAppId { get; set; }
        public int RequestTimeout { get; set; }
        public int ShardTimeout { get; set; }
        public string[] BundledPluginIds { get; set; }
        public bool VerifySsl { get; set; } // TODO

        public string AzureAdAudience { get; set; }
        public string AzureAdTenant { get; set; }
    }
}
