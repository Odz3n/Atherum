using Microsoft.AspNetCore.SignalR;
using WebTranslator.Hubs;
using WebTranslator.Models.Azure;
using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.ImageTranslation;
using WebTranslator.Models.TranslationDTOs.TextTranslation;
using WebTranslator.Services.AIVisionService;
using WebTranslator.Services.Api;
using WebTranslator.Services.Azure;
using WebTranslator.Services.ErrorHandling;
using WebTranslator.Services.Routing;
using WebTranslator.Services.Status;
using WebTranslator.Services.Validation;

namespace WebTranslator.Services.TranslationService
{
    public class TranslationService
    {
        private readonly ILogger<TranslationService> _logger;
        private readonly ITextValidator _textValidator;
        private readonly IImageValidator _imageValidator;
        private readonly IApiConfiguration<TranslationApiConfig> _apiConfiguration;
        private readonly IErrorHandler _errorHandler;
        private readonly Routing.IRouteBuilder _routeBuilder;
        private readonly IAzureTranslationClient _azureTranslationClient;
        private readonly IStatusSender _statusSender;
        private readonly IVisionService _visionService;

        private TranslationConfig _translationConfig;
        private readonly SemaphoreSlim _processLock = new SemaphoreSlim(1, 1);

        private const double PROGRESS_DETECTING_LANGUAGE = 20;
        private const double PROGRESS_TRANSLATING = 50;
        private const double PROGRESS_LANGUAGE_DETECTED = 70;
        private const double PROGRESS_COMPLETE = 100;

        public TranslationService(
            TranslationConfig translationConfig,
            ILogger<TranslationService> logger,
            ITextValidator textValidator,
            IImageValidator imageValidator,
            IApiConfiguration<TranslationApiConfig> apiConfiguration,
            IErrorHandler errorHandler,
            Routing.IRouteBuilder routeBuilder,
            IAzureTranslationClient azureTranslationClient,
            IStatusSender statusSender,
            IVisionService visionService)
        {
            _translationConfig = translationConfig;
            _logger = logger;
            _textValidator = textValidator;
            _imageValidator = imageValidator;
            _apiConfiguration = apiConfiguration;
            _errorHandler = errorHandler;
            _routeBuilder = routeBuilder;
            _azureTranslationClient = azureTranslationClient;
            _statusSender = statusSender;
            _visionService = visionService;
        }

        public void SetConfig(TranslationConfig config)
        {
            _translationConfig = config;
            _logger.LogInformation($"Translation config updated: From={config.LangFrom}, To={string.Join(",", config.LangsTo)}");
        }

        public async Task ProcessTextAsync(string text, string? sessionId = null)
        {
            await _processLock.WaitAsync();
            try
            {
                _logger.LogInformation($"Processing text: '{text}', Session: {sessionId}");

                if (string.IsNullOrEmpty(text))
                {
                    await HandleTextCleared(sessionId);
                    return;
                }

                var validationResult = _textValidator.Validate(text);
                if (!validationResult.IsValid)
                {
                    await _statusSender.SendStatusAsync(
                        validationResult.Type,
                        validationResult.Message,
                        text,
                        sessionId,
                        isError: true
                    );
                    return;
                }

                await _statusSender.SendStatusAsync(
                    MessageType.Valid,
                    "Text validated",
                    text,
                    sessionId
                );

                await TranslateAsync(text, sessionId);
            }
            finally
            {
                _processLock.Release();
            }
        }

        public async Task ProcessImageAsync(string imageData, string? sessionId = null)
        {
            try
            {
                _logger.LogInformation($"Processing image for session: {sessionId}");

                var validationResult = _imageValidator.Validate(imageData);
                if (!validationResult.IsValid)
                {
                    await _statusSender.SendStatusAsync(
                        validationResult.ErrorType,
                        validationResult.ErrorMessage ?? "Invalid image",
                        null,
                        sessionId,
                        isError: true,
                        isImage: true
                    );
                    return;
                }

                var imageBytes = validationResult.ImageBytes!;

                await _statusSender.SendStatusAsync(
                    MessageType.DetectingLanguage,
                    "🔍 Extracting text from image...",
                    null,
                    sessionId,
                    ocrProgress: 20,
                    imagePreview: imageData,
                    isImage: true
                );

                var ocrResult = await _visionService.ExtractTextAsync(imageBytes);

                if (string.IsNullOrWhiteSpace(ocrResult.Text))
                {
                    await _statusSender.SendStatusAsync(
                        MessageType.ValidationError,
                        "No text found in image",
                        null,
                        sessionId,
                        ocrProgress: 100,
                        imagePreview: imageData,
                        isError: true,
                        isImage: true
                    );
                    return;
                }

                await _statusSender.SendStatusAsync(
                    MessageType.LanguageDetected,
                    $"📝 Text extracted: {ocrResult.WordCount} words",
                    ocrResult.Text,
                    sessionId,
                    ocrProgress: 50,
                    ocrConfidence: ocrResult.Confidence,
                    imagePreview: imageData,
                    isImage: true
                );

                var (translations, detectedLanguage) = await TranslateImageTextAsync(
                    ocrResult.Text,
                    _translationConfig.LangFrom,
                    _translationConfig.LangsTo
                );

                await _statusSender.SendStatusAsync(
                    MessageType.Translated,
                    "✅ Translation complete",
                    ocrResult.Text,
                    sessionId,
                    translatedText: translations?.FirstOrDefault()?.Text,
                    detectedLanguage: detectedLanguage,
                    targetLanguages: _translationConfig.LangsTo,
                    allTranslations: translations,
                    ocrProgress: 100,
                    ocrConfidence: ocrResult.Confidence,
                    imagePreview: imageData,
                    isImage: true
                );

                _logger.LogInformation($"Image translation completed for session: {sessionId}");
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleErrorAsync(
                    ex,
                    "Image translation failed",
                    sessionId,
                    (type, message, originalText, sId, isError) =>
                        _statusSender.SendStatusAsync(
                            type,
                            message,
                            originalText,
                            sId,
                            isError: true,
                            isImage: true,
                            ocrProgress: 0,
                            ocrConfidence: 0,
                            imagePreview: null,
                            extractedText: null
                        ),
                    isImage: true
                );
            }
        }

        private async Task<(List<TranslationDto>? Translations, string? DetectedLanguage)> TranslateImageTextAsync(
            string text,
            string fromLanguage,
            List<string> toLanguages)
        {
            var originalConfig = _translationConfig;

            try
            {
                _translationConfig.LangFrom = fromLanguage;
                _translationConfig.LangsTo = toLanguages;

                var result = await TranslateAsync(text);

                if (result == null || !result.Any())
                    return (null, null);

                var root = result.First();

                var detectedLanguage = root.DetectedLanguage?.Language;
                var translations = root.Translations?.Select(t => new TranslationDto
                {
                    Text = t.Text,
                    ToLanguage = t.To
                }).ToList();

                return (translations, detectedLanguage);
            }
            finally
            {
                _translationConfig = originalConfig;
            }
        }

        public async Task<List<Root>?> TranslateAsync(string text, string? sessionId = null)
        {
            try
            {
                if (_translationConfig.LangFrom == "auto")
                {
                    await _statusSender.SendStatusAsync(
                        MessageType.DetectingLanguage,
                        "Detecting source language...",
                        text,
                        sessionId,
                        progress: PROGRESS_DETECTING_LANGUAGE
                    );
                }

                string route = _routeBuilder.BuildTranslateRoute(
                    _translationConfig.LangFrom,
                    _translationConfig.LangsTo);

                var apiConfig = _apiConfiguration.GetConfig();

                if (apiConfig == null)
                {
                    await _statusSender.SendStatusAsync(
                        MessageType.ApiError,
                        "Translation API configuration missing",
                        text,
                        sessionId,
                        isError: true
                    );
                    return null;
                }

                await _statusSender.SendStatusAsync(
                    MessageType.Translating,
                    "Translating...",
                    text,
                    sessionId,
                    progress: PROGRESS_TRANSLATING
                );

                var result = await _azureTranslationClient.TranslateAsync(
                    text,
                    apiConfig.Endpoint,
                    apiConfig.ApiKey,
                    route,
                    apiConfig.Location);

                if (result == null || !result.Any())
                {
                    await _statusSender.SendStatusAsync(
                        MessageType.TranslationFailed,
                        "Translation failed - no result",
                        text,
                        sessionId,
                        isError: true
                    );
                    return null;
                }

                var root = result.First();
                string? detectedLanguage = root.DetectedLanguage?.Language;
                string? detectedScore = root.DetectedLanguage?.Score;

                if (!string.IsNullOrEmpty(detectedLanguage))
                {
                    await _statusSender.SendStatusAsync(
                        MessageType.LanguageDetected,
                        $"Detected: {detectedLanguage} (confidence: {detectedScore})",
                        text,
                        sessionId,
                        detectedLanguage: detectedLanguage,
                        detectedLanguageScore: double.TryParse(detectedScore, out var score) ? score : null,
                        progress: PROGRESS_LANGUAGE_DETECTED
                    );
                }

                var translations = root.Translations ?? new List<Translation>();
                var translatedText = translations.FirstOrDefault()?.Text ?? string.Empty;

                if (!string.IsNullOrEmpty(translatedText))
                {
                    var allTranslations = translations.Select(t => new TranslationDto
                    {
                        Text = t.Text,
                        ToLanguage = t.To
                    }).ToList();

                    await _statusSender.SendStatusAsync(
                        MessageType.Translated,
                        "Translation complete",
                        text,
                        sessionId,
                        translatedText: translatedText,
                        detectedLanguage: detectedLanguage,
                        targetLanguages: _translationConfig.LangsTo,
                        allTranslations: allTranslations,
                        progress: PROGRESS_COMPLETE
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleErrorAsync(
                    ex,
                    text,
                    sessionId,
                    (type, message, originalText, sId, isError) =>
                        _statusSender.SendStatusAsync(
                            type,
                            message,
                            originalText,
                            sId,
                            isError: isError
                        )
                );
                return null;
            }
        }

        public async Task ClearTranslationsAsync(string? sessionId = null)
        {
            await _statusSender.SendStatusAsync(
                MessageType.Idle,
                "Ready for input",
                null,
                sessionId,
                isClearing: true
            );

            _logger.LogInformation($"Translations cleared for session: {sessionId}");
        }

        private async Task HandleTextCleared(string? sessionId)
        {
            await _statusSender.SendStatusAsync(
                MessageType.Clearing,
                "Text cleared",
                null,
                sessionId,
                isClearing: true
            );

            await _statusSender.SendStatusAsync(
                MessageType.Idle,
                "Ready for input",
                null,
                sessionId,
                isClearing: true
            );
        }
    }
}