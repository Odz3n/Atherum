using Microsoft.AspNetCore.SignalR;
using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.ImageTranslation;
using WebTranslator.Models.TranslationDTOs.TextTranslation;
using WebTranslator.Services.TranslationService;

namespace WebTranslator.Hubs;

public class TranslationHub: Hub
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
        _logger.LogInformation($"Processing text from {sessionId}: '{request.Text}' (Action: {request.Action})");

        try
        {
            if (request.ToLanguages.Any())
            {
                _translationService.SetConfig(new TranslationConfig
                {
                    LangFrom = request.FromLanguage ?? "auto",
                    LangsTo = request.ToLanguages,
                    Location = "germanywestcentral"
                });
            }
            await _translationService.ProcessTextAsync(request.Text, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing text from {sessionId}");
            await Clients.Client(sessionId).SendAsync("TranslationError", new
            {
                message = ex.Message,
                type = "processing_error",
                timestamp = DateTime.UtcNow
            });
        }
    }
    public async Task ProcessImage(ImageTranslationRequest request)
    {
        var sessionId = Context.ConnectionId;
        _logger.LogInformation($"Processing image from {sessionId}");

        try
        {
            if (request.ToLanguages.Any())
            {
                _translationService.SetConfig(new TranslationConfig
                {
                    LangFrom = request.FromLanguage ?? "auto",
                    LangsTo = request.ToLanguages,
                    Location = "germanywestcentral"
                });
            }

            await _translationService.ProcessImageAsync(request.ImageData, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing image from {sessionId}");
            await Clients.Client(sessionId).SendAsync("TranslationError", new
            {
                message = ex.Message,
                type = "image_processing_error",
                timestamp = DateTime.UtcNow
            });
        }
    }
    public async Task ClearText()
    {
        var sessionId = Context.ConnectionId;
        _logger.LogInformation($"Clearing text for {sessionId}");

        await _translationService.ClearTranslationsAsync(sessionId);
    }
    public async Task GetStatus()
    {
        var sessionId = Context.ConnectionId;
        _logger.LogInformation($"Status requested by {sessionId}");

        await Clients.Client(sessionId).SendAsync("TranslationStatus", new TranslationStatusDto
        {
            Type = MessageType.Info,
            Message = "Service is running",
            Timestamp = DateTime.UtcNow
        });
    }

    //------------------------------
    public override async Task OnConnectedAsync()
    {
        var sessionId = Context.ConnectionId;
        _logger.LogInformation($"Client connected: {sessionId}");

        await Clients.Client(sessionId).SendAsync("Connected", new
        {
            sessionId,
            message = "Connected to translation service",
            timestamp = DateTime.UtcNow
        });

        await Clients.Client(sessionId).SendAsync("TranslationStatus", new TranslationStatusDto
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
        _logger.LogInformation($"Client disconnected: {sessionId}");

        if (exception != null)
        {
            _logger.LogError(exception, $"Client {sessionId} disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
