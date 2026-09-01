using Azure;

namespace WebTranslator.Services.Api
{
    public class AzureVisionApiConfig
    {
        public Uri? Endpoint { get; set; }
        public AzureKeyCredential? AzureKeyCredential { get; set; }
        public string Location { get; set; } = "germanywestcentral";
        public int TimeoutSeconds { get; set; } = 30;
    }
    public class AzureVisionApiConfiguration : IApiConfiguration<AzureVisionApiConfig>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureVisionApiConfiguration> _logger;

        public AzureVisionApiConfiguration(
            IConfiguration configuration,
            ILogger<AzureVisionApiConfiguration> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }
        AzureVisionApiConfig? IApiConfiguration<AzureVisionApiConfig>.GetConfig()
        {
            var endpoint = _configuration["TranslationAPI:Endpoint"];
            var apiKey = _configuration["TranslationAPI:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Translation API configuration is missing");
                return null;
            }

            return new AzureVisionApiConfig
            {
                Endpoint = new Uri(_configuration["AzureVision:Endpoint"]!),
                AzureKeyCredential = new AzureKeyCredential(_configuration["AzureVision:ApiKey"]!),
                Location = "germanywestcentral",
                TimeoutSeconds = 30
            };
        }
    }
}
