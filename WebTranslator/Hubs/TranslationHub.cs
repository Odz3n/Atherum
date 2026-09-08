using Microsoft.AspNetCore.SignalR;
using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.ImageTranslation;
using WebTranslator.Models.TranslationDTOs.TextTranslation;
using WebTranslator.Models.TranslationDTOs.Vision;
using WebTranslator.Services.TranslationService;

namespace WebTranslator.Hubs;

public class TranslationHub : Hub
{
    private readonly TranslationService _translationService;
    private readonly ILogger<TranslationHub> _logger;

    public TranslationHub(
        TranslationService translationService,
        ILogger<TranslationHub> logger)
    {
        _translationService = translationService;
        _logger = logger;
    }

    public async Task ProcessText(TranslationRequestDto request)
    {
        var sessionId = Context.ConnectionId;

        if (request == null)
        {
            await SendError(
                sessionId,
                "Invalid translation request",
                "processing_error");

            return;
        }

        _logger.LogInformation(
            "Processing text from {SessionId}: '{Text}' (Action: {Action})",
            sessionId,
            request.Text,
            request.Action);

        try
        {
            var fromLanguage = string.IsNullOrWhiteSpace(request.FromLanguage)
                ? "auto"
                : request.FromLanguage;

            var toLanguages = request.ToLanguages?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];

            if (toLanguages.Count == 0)
            {
                await SendError(
                    sessionId,
                    "Please select at least one target language",
                    "validation_error");

                return;
            }

            _translationService.SetConfig(new TranslationConfig
            {
                LangFrom = fromLanguage,
                LangsTo = toLanguages,
                Location = "germanywestcentral"
            });

            await _translationService.ProcessTextAsync(
                request.Text ?? string.Empty,
                sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing text from {SessionId}",
                sessionId);

            await SendError(
                sessionId,
                ex.Message,
                "processing_error");
        }
    }

    public async Task ProcessImage(ImageTranslationRequest request)
    {
        var sessionId = Context.ConnectionId;

        if (request == null)
        {
            await SendError(
                sessionId,
                "Invalid image request",
                "image_processing_error");

            return;
        }

        _logger.LogInformation(
            "Processing image from {SessionId}",
            sessionId);

        try
        {
            var fromLanguage = string.IsNullOrWhiteSpace(request.FromLanguage)
                ? "auto"
                : request.FromLanguage;

            var toLanguages = request.ToLanguages?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];

            if (toLanguages.Count == 0)
            {
                await SendError(
                    sessionId,
                    "Please select at least one target language",
                    "validation_error");

                return;
            }

            if (string.IsNullOrWhiteSpace(request.ImageData))
            {
                await SendError(
                    sessionId,
                    "Image data is empty",
                    "validation_error");

                return;
            }

            _translationService.SetConfig(new TranslationConfig
            {
                LangFrom = fromLanguage,
                LangsTo = toLanguages,
                Location = "germanywestcentral"
            });

            await _translationService.ProcessImageAsync(
                request.ImageData,
                sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing image from {SessionId}",
                sessionId);

            await SendError(
                sessionId,
                ex.Message,
                "image_processing_error");
        }
    }

    public async Task ProcessImageAnalysis(ImageAnalysisRequest request)
    {
        var sessionId = Context.ConnectionId;

        if (request == null)
        {
            await SendAnalysisError(
                sessionId,
                "Invalid image analysis request");

            return;
        }

        _logger.LogInformation(
            "Processing image analysis from {SessionId}",
            sessionId);

        try
        {
            if (string.IsNullOrWhiteSpace(request.ImageData))
            {
                await SendAnalysisError(
                    sessionId,
                    "Image data is empty");

                return;
            }

            var fromLanguage = string.IsNullOrWhiteSpace(request.FromLanguage)
                ? "auto"
                : request.FromLanguage;

            var toLanguages = request.ToLanguages?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];

            if (toLanguages.Count == 0)
            {
                toLanguages = ["en"];
            }

            _translationService.SetConfig(new TranslationConfig
            {
                LangFrom = fromLanguage,
                LangsTo = toLanguages,
                Location = "germanywestcentral"
            });

            var options = request.Options
                ?? AnalysisOptions.AllFeatures(
                    fromLanguage,
                    toLanguages);

            options.Processing ??= new ProcessingOptions();

            options.Processing.LangFrom =
                string.IsNullOrWhiteSpace(options.Processing.LangFrom)
                    ? fromLanguage
                    : options.Processing.LangFrom;

            options.Processing.LangsTo =
                options.Processing.LangsTo?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                ?? toLanguages;

            await _translationService.ProcessImageWithAnalysisAsync(
                request.ImageData,
                sessionId,
                options);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing image analysis from {SessionId}",
                sessionId);

            await SendAnalysisError(
                sessionId,
                ex.Message);
        }
    }

    public async Task ClearText()
    {
        var sessionId = Context.ConnectionId;

        _logger.LogInformation(
            "Clearing text for {SessionId}",
            sessionId);

        try
        {
            await _translationService.ClearTranslationsAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error clearing text for {SessionId}",
                sessionId);

            await SendError(
                sessionId,
                ex.Message,
                "clear_error");
        }
    }

    public async Task GetStatus()
    {
        var sessionId = Context.ConnectionId;

        _logger.LogInformation(
            "Status requested by {SessionId}",
            sessionId);

        await Clients.Client(sessionId).SendAsync(
            "TranslationStatus",
            new TranslationStatusDto
            {
                Type = MessageType.Info,
                Message = "Service is running",
                Timestamp = DateTime.UtcNow
            });
    }

    public override async Task OnConnectedAsync()
    {
        var sessionId = Context.ConnectionId;

        _logger.LogInformation(
            "Client connected: {SessionId}",
            sessionId);

        await Clients.Client(sessionId).SendAsync(
            "Connected",
            new
            {
                sessionId,
                message = "Connected to translation service",
                timestamp = DateTime.UtcNow
            });

        await Clients.Client(sessionId).SendAsync(
            "TranslationStatus",
            new TranslationStatusDto
            {
                Type = MessageType.Idle,
                Message = "Ready for translation",
                Timestamp = DateTime.UtcNow
            });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sessionId = Context.ConnectionId;

        _logger.LogInformation(
            "Client disconnected: {SessionId}",
            sessionId);

        if (exception != null)
        {
            _logger.LogError(
                exception,
                "Client {SessionId} disconnected with error",
                sessionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task SendError(
        string sessionId,
        string message,
        string type)
    {
        await Clients.Client(sessionId).SendAsync(
            "TranslationError",
            new
            {
                message,
                type,
                timestamp = DateTime.UtcNow
            });
    }

    private async Task SendAnalysisError(
        string sessionId,
        string message)
    {
        await Clients.Client(sessionId).SendAsync(
            "AnalysisError",
            new
            {
                message,
                type = "analysis_error",
                timestamp = DateTime.UtcNow,
                isError = true
            });
    }
}