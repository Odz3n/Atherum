namespace WebTranslator.Services.Api
{
    public class BlobStorageApiConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Location { get; set; } = "germanywestcentral";
        public int TimeoutSeconds { get; set; } = 30;
    }
    public class AzureBlobStorageApiConfiguration : IApiConfiguration<BlobStorageApiConfig>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureBlobStorageApiConfiguration> _logger;
        public AzureBlobStorageApiConfiguration(
            IConfiguration configuration,
            ILogger<AzureBlobStorageApiConfiguration> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public BlobStorageApiConfig? GetConfig()
        {
            var connectionString = _configuration["BlobStorage:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Blob Storage API configuration is missing");
                return null;
            }

            return new BlobStorageApiConfig
            {
                ConnectionString = connectionString,
                Location = "germanywestcentral",
                TimeoutSeconds = 30
            };
        }
    }
}
