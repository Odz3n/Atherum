using Microsoft.AspNetCore.SignalR;
using WebTranslator.Hubs;
using WebTranslator.Models.Azure;
using WebTranslator.Models.Status;
using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.TextTranslation;
using WebTranslator.Models.TranslationDTOs.Vision;
using WebTranslator.Services.AzureBlobStorage;
using WebTranslator.Services.AIVisionService;
using WebTranslator.Services.Api;
using WebTranslator.Services.Azure;
using WebTranslator.Services.ErrorHandling;
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
        private readonly AzureBlobStorageService _azureBlobStorageService;

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
            IVisionService visionService,
            AzureBlobStorageService azureBlobStorageService,
            IHubContext<TranslationHub> hubContext)
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
            _azureBlobStorageService = azureBlobStorageService;
        }

        public void SetConfig(TranslationConfig config)
        {
            _translationConfig = config;

            _logger.LogInformation(
                $"Translation config updated: From={config.LangFrom}, To={string.Join(",", config.LangsTo)}");
        }

        public async Task ProcessTextAsync(string text, string? sessionId = null)
        {
            await _processLock.WaitAsync();

            try
            {
                _logger.LogInformation(
                    $"Processing text: '{text}', Session: {sessionId}");

                if (string.IsNullOrEmpty(text))
                {
                    await HandleTextCleared(sessionId);
                    return;
                }

                var validationResult = _textValidator.Validate(text);

                if (!validationResult.IsValid)
                {
                    await _statusSender.SendStatusAsync(
                        new StatusPayload(
                            Type: validationResult.Type,
                            Message: validationResult.Message,
                            OriginalText: text,
                            SessionId: sessionId,
                            IsError: true));

                    return;
                }

                await _statusSender.SendStatusAsync(
                    new StatusPayload(
                        Type: MessageType.Valid,
                        Message: "Text validated",
                        OriginalText: text,
                        SessionId: sessionId));

                await TranslateAsync(text, sessionId);
            }
            finally
            {
                _processLock.Release();
            }
        }

        public async Task ProcessImageAsync(
            string imageData,
            string? sessionId = null)
        {
            try
            {
                _logger.LogInformation(
                    $"Processing image for session: {sessionId}");

                var validationResult =
                    _imageValidator.Validate(imageData);

                if (!validationResult.IsValid)
                {
                    await _statusSender.SendStatusAsync(
                        new StatusPayload(
                            Type: validationResult.ErrorType,
                            Message: validationResult.ErrorMessage ?? "Invalid image",
                            SessionId: sessionId,
                            IsError: true,
                            IsImage: true));

                    return;
                }

                var imageBytes = validationResult.ImageBytes!;

                await _statusSender.SendStatusAsync(
                    new StatusPayload(
                        Type: MessageType.DetectingLanguage,
                        Message: "🔍 Extracting text from image...",
                        SessionId: sessionId,
                        OcrProgress: 20,
                        ImagePreview: imageData,
                        IsImage: true));

                var ocrResult =
                    await _visionService.ExtractTextAsync(imageBytes);

                if (string.IsNullOrWhiteSpace(ocrResult.Text))
                {
                    await _statusSender.SendStatusAsync(
                        new StatusPayload(
                            Type: MessageType.ValidationError,
                            Message: "No text found in image",
                            SessionId: sessionId,
                            IsError: true,
                            OcrProgress: 100,
                            ImagePreview: imageData,
                            IsImage: true));

                    return;
                }

                await _statusSender.SendStatusAsync(
                    new StatusPayload(
                        Type: MessageType.LanguageDetected,
                        Message: $"📝 Text extracted: {ocrResult.WordCount} words",
                        OriginalText: ocrResult.Text,
                        SessionId: sessionId,
                        OcrProgress: 50,
                        OcrConfidence: ocrResult.Confidence,
                        ImagePreview: imageData,
                        IsImage: true));

                var (translations, detectedLanguage) =
                    await TranslateImageTextAsync(
                        ocrResult.Text,
                        _translationConfig.LangFrom,
                        _translationConfig.LangsTo);

                await _statusSender.SendStatusAsync(
                    new StatusPayload(
                        Type: MessageType.Translated,
                        Message: "✅ Translation complete",
                        OriginalText: ocrResult.Text,
                        SessionId: sessionId,
                        TranslatedText: translations?.FirstOrDefault()?.Text,
                        DetectedLanguage: detectedLanguage,
                        TargetLanguages: _translationConfig.LangsTo,
                        AllTranslations: translations,
                        OcrProgress: 100,
                        OcrConfidence: ocrResult.Confidence,
                        ImagePreview: imageData,
                        IsImage: true));

                _logger.LogInformation(
                    $"Image translation completed for session: {sessionId}");
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleErrorAsync(
                    ex,
                    "Image translation failed",
                    sessionId,
                    (type, message, originalText, sId, isError) =>
                        _statusSender.SendStatusAsync(
                            new StatusPayload(
                                Type: type,
                                Message: message,
                                OriginalText: originalText,
                                SessionId: sId,
                                IsError: true,
                                IsImage: true)),
                    isImage: true);
            }
        }

        public async Task ProcessImageWithAnalysisAsync(
            string imageData,
            string? sessionId = null,
            AnalysisOptions? options = null)
        {
            try
            {
                _logger.LogInformation($"Processing image with analysis for session: {sessionId}");

                var validationResult = _imageValidator.Validate(imageData);
                if (!validationResult.IsValid)
                {
                    await _statusSender.SendAnalysisStatusAsync(
                        type: MessageType.ValidationError,
                        message: validationResult.ErrorMessage ?? "Invalid image",
                        sessionId: sessionId,
                        isError: true);
                    return;
                }

                var imageBytes = validationResult.ImageBytes!;

                if (options == null)
                {
                    options = AnalysisOptions.FromTranslationConfig(_translationConfig);
                }
                else if (options.Processing != null)
                {
                    if (string.IsNullOrEmpty(options.Processing.LangFrom))
                        options.Processing.LangFrom = _translationConfig.LangFrom;

                    if (options.Processing.LangsTo == null || !options.Processing.LangsTo.Any())
                        options.Processing.LangsTo = _translationConfig.LangsTo;
                }

                await _statusSender.SendAnalysisStatusAsync(
                    type: MessageType.DetectingLanguage,
                    message: "Analyzing image with AI...",
                    sessionId: sessionId,
                    progress: 30,
                    imagePreview: imageData);

                var analysisResult = await _visionService.AnalyzeImageAsync(imageBytes, options);

                await _statusSender.SendAnalysisStatusAsync(
                    type: MessageType.LanguageDetected,
                    message: $"Found {analysisResult.Objects.Count} objects and {analysisResult.Tags.Count} tags",
                    sessionId: sessionId,
                    progress: 50,
                    imagePreview: imageData,
                    extractedText: analysisResult.ExtractedText,
                    objects: analysisResult.Objects,
                    tags: analysisResult.Tags);

                var fromLang = options.Processing?.LangFrom ?? _translationConfig.LangFrom;
                var toLangs = options.Processing?.LangsTo ?? _translationConfig.LangsTo;

                if (!string.IsNullOrWhiteSpace(analysisResult.ExtractedText) &&
                    options.Processing?.TranslateExtractedText == true)
                {
                    await _statusSender.SendAnalysisStatusAsync(
                        type: MessageType.Translating,
                        message: "Translating extracted text...",
                        sessionId: sessionId,
                        progress: 70,
                        imagePreview: imageData,
                        extractedText: analysisResult.ExtractedText,
                        objects: analysisResult.Objects,
                        tags: analysisResult.Tags);

                    var (translations, detectedLanguage) = await TranslateImageTextAsync(
                        analysisResult.ExtractedText,
                        fromLang,
                        toLangs);

                    analysisResult.Translations = translations ?? new List<TranslationDto>();
                    analysisResult.DetectedLanguage = detectedLanguage;
                }

                if (options.Processing?.TranslateObjectNames == true && analysisResult.Objects.Any())
                {
                    await _statusSender.SendAnalysisStatusAsync(
                        type: MessageType.Translating,
                        message: "Translating object names...",
                        sessionId: sessionId,
                        progress: 75,
                        imagePreview: imageData,
                        objects: analysisResult.Objects);

                    await TranslateObjectNamesAsync(analysisResult.Objects, fromLang, toLangs);
                }

                if (options.Processing?.TranslateTagNames == true && analysisResult.Tags.Any())
                {
                    await _statusSender.SendAnalysisStatusAsync(
                        type: MessageType.Translating,
                        message: "Translating tags...",
                        sessionId: sessionId,
                        progress: 80,
                        imagePreview: imageData,
                        tags: analysisResult.Tags);

                    await TranslateTagNamesAsync(analysisResult.Tags, fromLang, toLangs);
                }

                if (options.Processing?.SaveToBlob == true)
                {
                    try
                    {
                        await _statusSender.SendAnalysisStatusAsync(
                            type: MessageType.Saving,
                            message: "💾 Saving image to cloud storage...",
                            sessionId: sessionId,
                            progress: 85);

                        var containerName = "images";

                        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
                        var timeIdentifier = DateTime.UtcNow.ToString("HH-mm-ss");
                        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);

                        var blobName = $"{timestamp}/{timeIdentifier}_{uniqueId}.jpg";

                        if (!_azureBlobStorageService.ContainerExists(containerName))
                            _azureBlobStorageService.CreateContainer(containerName);

                        var container = _azureBlobStorageService.GetContainer(containerName);
                        var blob = container.GetBlobClient(blobName);

                        using var stream = new MemoryStream(imageBytes);
                        await blob.UploadAsync(stream, overwrite: true);

                        string? downloadUrl = null;
                        if (options.Processing?.ReturnDownloadUrl == true)
                        {
                            try
                            {
                                var sasUri = _azureBlobStorageService.CreateBlobSASURI(
                                    containerName,
                                    blobName,
                                    DateTimeOffset.UtcNow.AddDays(7),
                                    BlobSasPermission.R);

                                downloadUrl = sasUri.ToString();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to generate SAS URL");
                            }
                        }

                        await _statusSender.SendAnalysisStatusAsync(
                            type: MessageType.Translated,
                            message: "✅ Image saved to cloud storage!",
                            sessionId: sessionId,
                            progress: 95,
                            imagePreview: imageData,
                            downloadUrl: downloadUrl);

                        _logger.LogInformation($"Image saved: {containerName}/{blobName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error saving image: {ex.Message}");

                        await _statusSender.SendAnalysisStatusAsync(
                            type: MessageType.ApiError,
                            message: $"❌ Failed to save image: {ex.Message}",
                            sessionId: sessionId,
                            isError: true,
                            progress: 85,
                            imagePreview: imageData);
                    }
                }

                await _statusSender.SendAnalysisStatusAsync(
                    type: MessageType.Translated,
                    message: "✅ Analysis and translation complete!",
                    sessionId: sessionId,
                    progress: 100,
                    imagePreview: imageData,
                    extractedText: analysisResult.ExtractedText,
                    detectedLanguage: analysisResult.DetectedLanguage,
                    objects: analysisResult.Objects,
                    tags: analysisResult.Tags,
                    translations: analysisResult.Translations);

                _logger.LogInformation($"Image analysis completed for session: {sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Image analysis failed for session: {sessionId}");
                await _statusSender.SendAnalysisStatusAsync(
                    type: MessageType.TranslationFailed,
                    message: $"Analysis failed: {ex.Message}",
                    sessionId: sessionId,
                    isError: true);
            }
        }

        public async Task<List<Root>?> TranslateAsync(
            string text,
            string? sessionId = null)
        {
            try
            {
                if (_translationConfig.LangFrom == "auto")
                {
                    await _statusSender.SendStatusAsync(
                        new StatusPayload(
                            Type: MessageType.DetectingLanguage,
                            Message: "Detecting source language...",
                            OriginalText: text,
                            SessionId: sessionId,
                            Progress: PROGRESS_DETECTING_LANGUAGE));
                }

                string route =
                    _routeBuilder.BuildTranslateRoute(
                        _translationConfig.LangFrom,
                        _translationConfig.LangsTo);

                var apiConfig =
                    _apiConfiguration.GetConfig();

                if (apiConfig == null)
                {
                    await _statusSender.SendStatusAsync(
                        new StatusPayload(
                            Type: MessageType.ApiError,
                            Message: "Translation API configuration missing",
                            OriginalText: text,
                            SessionId: sessionId,
                            IsError: true));

                    return null;
                }

                await _statusSender.SendStatusAsync(
                    new StatusPayload(
                        Type: MessageType.Translating,
                        Message: "Translating...",
                        OriginalText: text,
                        SessionId: sessionId,
                        Progress: PROGRESS_TRANSLATING));

                var result =
                    await _azureTranslationClient.TranslateAsync(
                        text,
                        apiConfig.Endpoint,
                        apiConfig.ApiKey,
                        route,
                        apiConfig.Location);

                if (result == null || !result.Any())
                {
                    await _statusSender.SendStatusAsync(
                        new StatusPayload(
                            Type: MessageType.TranslationFailed,
                            Message: "Translation failed - no result",
                            OriginalText: text,
                            SessionId: sessionId,
                            IsError: true));

                    return null;
                }

                var root = result.First();

                string? detectedLanguage =
                    root.DetectedLanguage?.Language;

                string? detectedScore =
                    root.DetectedLanguage?.Score;

                if (!string.IsNullOrEmpty(detectedLanguage))
                {
                    await _statusSender.SendStatusAsync(
                        new StatusPayload(
                            Type: MessageType.LanguageDetected,
                            Message:
                                $"Detected: {detectedLanguage} (confidence: {detectedScore})",
                            OriginalText: text,
                            SessionId: sessionId,
                            DetectedLanguage: detectedLanguage,
                            DetectedLanguageScore:
                                double.TryParse(
                                    detectedScore,
                                    out var score)
                                    ? score
                                    : null,
                            Progress:
                                PROGRESS_LANGUAGE_DETECTED));
                }

                var translations =
                    root.Translations ??
                    new List<Translation>();

                var translatedText =
                    translations
                        .FirstOrDefault()?.Text ??
                    string.Empty;

                if (!string.IsNullOrEmpty(translatedText))
                {
                    var allTranslations =
                        translations
                            .Select(t => new TranslationDto
                            {
                                Text = t.Text,
                                ToLanguage = t.To
                            })
                            .ToList();

                    await _statusSender.SendStatusAsync(
                        new StatusPayload(
                            Type: MessageType.Translated,
                            Message: "Translation complete",
                            OriginalText: text,
                            SessionId: sessionId,
                            TranslatedText: translatedText,
                            DetectedLanguage: detectedLanguage,
                            TargetLanguages:
                                _translationConfig.LangsTo,
                            AllTranslations:
                                allTranslations,
                            Progress:
                                PROGRESS_COMPLETE));
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
                            new StatusPayload(
                                Type: type,
                                Message: message,
                                OriginalText: originalText,
                                SessionId: sId,
                                IsError: isError)));

                return null;
            }
        }

        private async Task<(List<TranslationDto>? Translations, string? DetectedLanguage)>TranslateImageTextAsync(
            string text,
            string fromLanguage,
            List<string> toLanguages)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (null, null);

            var languages = toLanguages?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();

            if (languages.Count == 0)
                return (null, null);

            var originalConfig = new TranslationConfig
            {
                LangFrom =
                    _translationConfig?.LangFrom ?? "auto",

                LangsTo =
                    new List<string>(
                        _translationConfig?.LangsTo ??
                        new List<string>()),

                Location =
                    _translationConfig?.Location ??
                    "germanywestcentral"
            };

            try
            {
                _translationConfig.LangFrom =
                    string.IsNullOrWhiteSpace(fromLanguage)
                        ? "auto"
                        : fromLanguage;

                _translationConfig.LangsTo =
                    new List<string>(languages);

                var result =
                    await TranslateAsync(text);

                if (result == null || !result.Any())
                    return (null, null);

                var root = result.First();

                var detectedLanguage =
                    root.DetectedLanguage?.Language;

                var translations =
                    root.Translations?
                        .Where(t =>
                            !string.IsNullOrWhiteSpace(t.Text))
                        .Select(t => new TranslationDto
                        {
                            Text = t.Text,
                            ToLanguage = t.To
                        })
                        .ToList();

                return (translations, detectedLanguage);
            }
            finally
            {
                _translationConfig = originalConfig;
            }
        }

        private async Task TranslateObjectNamesAsync(
            List<VisionObject> objects,
            string fromLanguage,
            List<string> targetLanguages)
        {
            if (objects == null ||
                objects.Count == 0 ||
                targetLanguages == null ||
                targetLanguages.Count == 0)
            {
                return;
            }

            await TranslateNamesAsync(
                objects,
                objectItem => objectItem.Name,
                (objectItem, translatedName) =>
                    objectItem.TranslatedName = translatedName,
                fromLanguage,
                targetLanguages);
        }

        private async Task TranslateTagNamesAsync(
            List<VisionTag> tags,
            string fromLanguage,
            List<string> targetLanguages)
        {
            if (tags == null ||
                tags.Count == 0 ||
                targetLanguages == null ||
                targetLanguages.Count == 0)
            {
                return;
            }

            await TranslateNamesAsync(
                tags,
                tag => tag.Name,
                (tag, translatedName) =>
                    tag.TranslatedName = translatedName,
                fromLanguage,
                targetLanguages);
        }

        private async Task TranslateNamesAsync<T>(
            List<T> items,
            Func<T, string?> getName,
            Action<T, string> setTranslatedName,
            string fromLanguage,
            List<string> targetLanguages)
        {
            var distinctNames = items
                .Select(getName)
                .Where(name =>
                    !string.IsNullOrWhiteSpace(name))
                .Select(name => name!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var translationsByName =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var name in distinctNames)
            {
                try
                {
                    var (translations, _) =
                        await TranslateImageTextAsync(
                            name,
                            fromLanguage,
                            targetLanguages);

                    var translatedName =
                        translations != null &&
                        translations.Count > 0
                            ? string.Join(
                                ", ",
                                translations.Select(t =>
                                    $"{t.Text} ({t.ToLanguage})"))
                            : string.Empty;

                    translationsByName[name] =
                        translatedName;

                    _logger.LogDebug(
                        "Translated '{Name}' -> '{Translation}'",
                        name,
                        translatedName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to translate '{Name}'",
                        name);

                    translationsByName[name] =
                        string.Empty;
                }
            }

            foreach (var item in items)
            {
                var name = getName(item);

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                name = name.Trim();

                if (translationsByName.TryGetValue(
                    name,
                    out var translatedName))
                {
                    setTranslatedName(
                        item,
                        translatedName);
                }
            }
        }

        public async Task ClearTranslationsAsync(
            string? sessionId = null)
        {
            await _statusSender.SendStatusAsync(
                new StatusPayload(
                    Type: MessageType.Idle,
                    Message: "Ready for input",
                    SessionId: sessionId,
                    IsClearing: true));

            _logger.LogInformation(
                $"Translations cleared for session: {sessionId}");
        }

        private async Task HandleTextCleared(
            string? sessionId)
        {
            await _statusSender.SendStatusAsync(
                new StatusPayload(
                    Type: MessageType.Clearing,
                    Message: "Text cleared",
                    SessionId: sessionId,
                    IsClearing: true));

            await _statusSender.SendStatusAsync(
                new StatusPayload(
                    Type: MessageType.Idle,
                    Message: "Ready for input",
                    SessionId: sessionId,
                    IsClearing: true));
        }
    }
}