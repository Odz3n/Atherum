using Microsoft.AspNetCore.SignalR;
using WebTranslator.Hubs;
using WebTranslator.Models.Status;
using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.ImageTranslation;
using WebTranslator.Models.TranslationDTOs.TextTranslation;
using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.Status
{

    public class StatusSender : IStatusSender
    {
        private readonly IHubContext<TranslationHub> _hubContext;
        private readonly ILogger<StatusSender> _logger;
        private readonly TranslationConfig _translationConfig;

        public StatusSender(
            IHubContext<TranslationHub> hubContext,
            ILogger<StatusSender> logger,
            TranslationConfig translationConfig)
        {
            _hubContext = hubContext;
            _logger = logger;
            _translationConfig = translationConfig;
        }

        public async Task SendAnalysisStatusAsync(
            MessageType type,
            string message,
            string? sessionId = null,
            int progress = 0,
            string? imagePreview = null,
            string? blobUri = null,
            string? extractedText = null,
            string? detectedLanguage = null,
            List<VisionObject>? objects = null,
            List<VisionTag>? tags = null,
            List<TranslationDto>? translations = null,
            string? downloadUrl = null,
            bool isError = false)
        {
            try
            {
                var status = new ImageAnalysisStatusDto
                {
                    Type = type,
                    Message = message,
                    Progress = progress,
                    ImagePreview = imagePreview,
                    BlobUri = blobUri,
                    ExtractedText = extractedText,
                    DetectedLanguage = detectedLanguage,
                    Objects = objects ?? new List<VisionObject>(),
                    Tags = tags ?? new List<VisionTag>(),
                    Translations = translations ?? new List<TranslationDto>(),
                    IsError = isError,
                    Timestamp = DateTime.UtcNow
                };

                if (!string.IsNullOrEmpty(sessionId))
                    await _hubContext.Clients.Client(sessionId).SendAsync("AnalysisStatus", status);
                else
                    await _hubContext.Clients.All.SendAsync("AnalysisStatus", status);

                _logger.LogInformation($"Analysis status sent: {type} - {message} (Session: {sessionId})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send analysis status: {type}");
            }
        }

        public async Task SendImageStatusAsync(
            ImageTranslationStatusDto status,
            string? sessionId = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(sessionId))
                    await _hubContext.Clients.Client(sessionId)
                        .SendAsync("ImageTranslationStatus", status);
                else
                    await _hubContext.Clients.All
                        .SendAsync("ImageTranslationStatus", status);

                _logger.LogInformation(
                   "Image status sent: {Type} - {Message} (Session: {SessionId})",
                   status.Type,
                   status.Message,
                   sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send image status: {Type}", status.Type);
            }
        }

        public async Task SendStatusAsync(
            StatusPayload payload)
        {
            if (payload.IsImage)
            {
                var status = new ImageTranslationStatusDto
                {
                    Type = payload.Type,
                    Message = payload.Message,
                    OriginalText = payload.OriginalText,
                    TranslatedText = payload.TranslatedText,
                    DetectedLanguage = payload.DetectedLanguage,
                    DetectedLanguageScore = payload.DetectedLanguageScore,
                    TargetLanguages = payload.TargetLanguages ?? _translationConfig.LangsTo,
                    AllTranslations = payload.AllTranslations,
                    Progress = payload.Progress,
                    IsClearing = payload.IsClearing,
                    IsError = payload.IsError,
                    ErrorDetails = payload.ErrorDetails,
                    ExtractedText = payload.ExtractedText ?? payload.OriginalText,
                    OcrProgress = payload.OcrProgress,
                    OcrConfidence = payload.OcrConfidence,
                    ImagePreview = payload.ImagePreview,
                    Timestamp = DateTime.UtcNow
                };

                await SendImageStatusAsync(status, payload.SessionId);
            }
            else
            {
                var status = new TranslationStatusDto
                {
                    Type = payload.Type,
                    Message = payload.Message,
                    OriginalText = payload.OriginalText,
                    TranslatedText = payload.TranslatedText,
                    DetectedLanguage = payload.DetectedLanguage,
                    DetectedLanguageScore = payload.DetectedLanguageScore,
                    TargetLanguages = payload.TargetLanguages ?? _translationConfig.LangsTo,
                    AllTranslations = payload.AllTranslations,
                    Progress = payload.Progress,
                    Timestamp = DateTime.UtcNow,
                    IsClearing = payload.IsClearing,
                    IsError = payload.IsError,
                    ErrorDetails = payload.ErrorDetails
                };

                await SendTextStatusAsync(status, payload.SessionId);
            }
        }

        public async Task SendTextStatusAsync(
            TranslationStatusDto status, 
            string? sessionId = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(sessionId))
                    await _hubContext.Clients.Client(sessionId)
                        .SendAsync("TranslationStatus", status);
                else
                    await _hubContext.Clients.All
                        .SendAsync("TranslationStatus", status);

                _logger.LogInformation(
                    "Status sent: {Type} - {Message} (Session: {SessionId})",
                    status.Type,
                    status.Message,
                    sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status: {Type}", status.Type);
            }
        }
    }
}
