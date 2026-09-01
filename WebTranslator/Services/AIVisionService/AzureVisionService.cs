using Azure;
using Azure.AI.Vision.ImageAnalysis;
using WebTranslator.Models.TranslationDTOs.Vision;
using WebTranslator.Services.Api;

namespace WebTranslator.Services.AIVisionService
{
    public class AzureVisionService: IVisionService
    {
        private readonly IApiConfiguration<AzureVisionApiConfig> _apiConfiguration;
        private readonly ILogger<AzureVisionService> _logger;
        private readonly ImageAnalysisClient _client;

        public AzureVisionService(
            IApiConfiguration<AzureVisionApiConfig> apiConfiguration,
            ILogger<AzureVisionService> logger)
        {
            _apiConfiguration = apiConfiguration;
            _logger = logger;

            var apiConfig = _apiConfiguration.GetConfig();
            _client = new ImageAnalysisClient(apiConfig?.Endpoint, apiConfig?.AzureKeyCredential);
        }
        public async Task<OcrResult> ExtractTextAsync(byte[] imageData)
        {
            try
            {
                _logger.LogInformation($"Processing image: {imageData.Length} bytes");

                var imageBinaryData = new BinaryData(imageData);

                var result = await _client.AnalyzeAsync(
                    imageBinaryData,
                    VisualFeatures.Read
                );

                if (result.Value.Read == null || result.Value.Read.Blocks.Count == 0)
                    return new OcrResult
                    {
                        Text = "No text found in image",
                        Confidence = 0,
                        Words = new List<OcrWord>()
                    };

                var extractedText = string.Join(" ", result.Value.Read.Blocks
                    .SelectMany(b => b.Lines)
                    .Select(l => l.Text));

                var words = result.Value.Read.Blocks
                    .SelectMany(b => b.Lines)
                    .SelectMany(l => l.Words)
                    .Select(w => new OcrWord
                    {
                        Text = w.Text,
                        Confidence = w.Confidence,
                        BoundingBox = new BoundingBox
                        {
                            X = (int)w.BoundingPolygon[0].X,
                            Y = (int)w.BoundingPolygon[0].Y,
                            Width = (int)(w.BoundingPolygon[2].X - w.BoundingPolygon[0].X),
                            Height = (int)(w.BoundingPolygon[2].Y - w.BoundingPolygon[0].Y)
                        }
                    }).ToList();

                return new OcrResult
                {
                    Text = extractedText,
                    Confidence = words.Any() ? words.Average(w => w.Confidence) : 0,
                    Words = words,
                    WordCount = words.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure Vision OCR failed");
                throw;
            }
        }
        public async Task<OcrResult> ExtractTextFromUrlAsync(string imageUrl)
        {
            using var client = new HttpClient();
            var imageData = await client.GetByteArrayAsync(imageUrl);
            return await ExtractTextAsync(imageData);
        }
    }
}
