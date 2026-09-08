using Azure.AI.Vision.ImageAnalysis;
using WebTranslator.Models.TranslationDTOs.Vision;
using WebTranslator.Services.AIVisionService.Handlers;
using WebTranslator.Services.Api;

namespace WebTranslator.Services.AIVisionService;

public class AzureVisionService : IVisionService
{
    private readonly IApiConfiguration<AzureVisionApiConfig> _apiConfiguration;
    private readonly ILogger<AzureVisionService> _logger;
    private readonly IAnalysisHandlerFactory _handlerFactory;
    private readonly ImageAnalysisClient _client;

    public AzureVisionService(
        IApiConfiguration<AzureVisionApiConfig> apiConfiguration,
        ILogger<AzureVisionService> logger,
        IAnalysisHandlerFactory handlerFactory)
    {
        _apiConfiguration = apiConfiguration;
        _logger = logger;
        _handlerFactory = handlerFactory;

        var apiConfig = _apiConfiguration.GetConfig();

        if (apiConfig?.Endpoint == null)
            throw new InvalidOperationException("Azure Vision endpoint is not configured.");

        if (apiConfig.AzureKeyCredential == null)
            throw new InvalidOperationException("Azure Vision API key is not configured.");

        _client = new ImageAnalysisClient(
            apiConfig.Endpoint,
            apiConfig.AzureKeyCredential);
    }

    public async Task<VisionAnalysisResult> AnalyzeImageAsync(
        byte[] imageData,
        AnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        if (imageData.Length == 0)
            throw new ArgumentException(
                "Image data cannot be empty.",
                nameof(imageData));

        options ??= AnalysisOptions.Default();

        try
        {
            _logger.LogInformation(
                "Analyzing image: {Bytes} bytes, Features: {Features}",
                imageData.Length,
                options.Features);

            var azureResult = await GetAzureAnalysisAsync(
                imageData,
                options.Features);

            var result = new VisionAnalysisResult
            {
                Timestamp = DateTime.UtcNow
            };

            var handlers = _handlerFactory.GetHandlers(options.Features);

            foreach (var handler in handlers)
            {
                await handler.ProcessAsync(
                    azureResult,
                    result,
                    options);
            }

            _logger.LogInformation(
                "Analysis complete: {Objects} objects, {Tags} tags, Text: {TextLength} chars",
                result.Objects.Count,
                result.Tags.Count,
                result.ExtractedText?.Length ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Image analysis failed");

            throw;
        }
    }

    public async Task<VisionAnalysisResult> AnalyzeImageFromUrlAsync(
        string imageUrl,
        AnalysisOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException(
                "Image URL cannot be empty.",
                nameof(imageUrl));

        try
        {
            _logger.LogInformation(
                "Analyzing image from URL: {ImageUrl}",
                imageUrl);

            using var client = new HttpClient();

            var imageData = await client.GetByteArrayAsync(imageUrl);

            return await AnalyzeImageAsync(
                imageData,
                options);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to analyze image from URL: {ImageUrl}",
                imageUrl);

            throw;
        }
    }

    public async Task<OcrResult> ExtractTextAsync(byte[] imageData)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        try
        {
            _logger.LogInformation(
                "Extracting text from image: {Bytes} bytes",
                imageData.Length);

            var options = AnalysisOptions.TextOnly();

            var result = await AnalyzeImageAsync(
                imageData,
                options);

            return new OcrResult
            {
                Text = result.ExtractedText,
                Confidence = result.Words.Count > 0
                    ? result.Words.Average(w => w.Confidence)
                    : 0,
                Words = result.Words,
                WordCount = result.Words.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Text extraction failed");

            throw;
        }
    }

    public async Task<OcrResult> ExtractTextFromUrlAsync(
        string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException(
                "Image URL cannot be empty.",
                nameof(imageUrl));

        _logger.LogInformation(
            "Extracting text from URL: {ImageUrl}",
            imageUrl);

        using var client = new HttpClient();

        var imageData = await client.GetByteArrayAsync(imageUrl);

        return await ExtractTextAsync(imageData);
    }

    private async Task<ImageAnalysisResult> GetAzureAnalysisAsync(
        byte[] imageData,
        AnalysisFeature features)
    {
        var binaryData = BinaryData.FromBytes(imageData);
        var visualFeatures = GetVisualFeatures(features);

        _logger.LogInformation(
            "Calling Azure Vision API with features: {VisualFeatures}",
            visualFeatures);

        return await _client.AnalyzeAsync(
            binaryData,
            visualFeatures);
    }

    private static VisualFeatures GetVisualFeatures(
        AnalysisFeature features)
    {
        var result = VisualFeatures.None;

        if (features.HasFlag(AnalysisFeature.Read))
            result |= VisualFeatures.Read;

        if (features.HasFlag(AnalysisFeature.Objects))
            result |= VisualFeatures.Objects;

        if (features.HasFlag(AnalysisFeature.Tags))
            result |= VisualFeatures.Tags;

        return result;
    }
}