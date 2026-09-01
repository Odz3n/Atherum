namespace WebTranslator.Services.Api
{
    public class TranslationApiConfig
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Location { get; set; } = "germanywestcentral";
        public int TimeoutSeconds { get; set; } = 30;
    }
    public class AzureTranslatorApiConfiguration : IApiConfiguration<TranslationApiConfig>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureTranslatorApiConfiguration> _logger;
        public AzureTranslatorApiConfiguration(
            IConfiguration configuration,
            ILogger<AzureTranslatorApiConfiguration> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public TranslationApiConfig? GetConfig()
        {
            var endpoint = _configuration["TranslationAPI:Endpoint"];
            var apiKey = _configuration["TranslationAPI:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Translation API configuration is missing");
                return null;
            }

            return new TranslationApiConfig
            {
                Endpoint = endpoint,
                ApiKey = apiKey,
                Location = "germanywestcentral",
                TimeoutSeconds = 30
            };
        }
    }
}
