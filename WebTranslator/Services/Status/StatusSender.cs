using Microsoft.AspNetCore.SignalR;
using WebTranslator.Hubs;
using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.ImageTranslation;
using WebTranslator.Models.TranslationDTOs.TextTranslation;

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
            MessageType type,
            string message,
            string? originalText = null,
            string? sessionId = null, 
            string? translatedText = null, 
            string? detectedLanguage = null, 
            double? detectedLanguageScore = null, 
            List<string>? targetLanguages = null, 
            List<TranslationDto>? allTranslations = null, 
            double? progress = null, 
            bool isClearing = false, 
            bool isError = false, 
            string? errorDetails = null, 
            int ocrProgress = 0, 
            double ocrConfidence = 0, 
            string? imagePreview = null, 
            string? extractedText = null, 
            bool isImage = false)
        {
            if (isImage)
            {
                var status = new ImageTranslationStatusDto
                {
                    Type = type,
                    Message = message,
                    OriginalText = originalText,
                    TranslatedText = translatedText,
                    DetectedLanguage = detectedLanguage,
                    DetectedLanguageScore = detectedLanguageScore,
                    TargetLanguages = targetLanguages ?? _translationConfig.LangsTo,
                    AllTranslations = allTranslations,
                    Progress = progress,
                    IsClearing = isClearing,
                    IsError = isError,
                    ErrorDetails = errorDetails,
                    ExtractedText = extractedText ?? originalText,
                    OcrProgress = ocrProgress,
                    OcrConfidence = ocrConfidence,
                    ImagePreview = imagePreview,
                    Timestamp = DateTime.UtcNow
                };

                await SendImageStatusAsync(status, sessionId);
            }
            else
            {
                var status = new TranslationStatusDto
                {
                    Type = type,
                    Message = message,
                    OriginalText = originalText,
                    TranslatedText = translatedText,
                    DetectedLanguage = detectedLanguage,
                    DetectedLanguageScore = detectedLanguageScore,
                    TargetLanguages = targetLanguages ?? _translationConfig.LangsTo,
                    AllTranslations = allTranslations,
                    Progress = progress,
                    Timestamp = DateTime.UtcNow,
                    IsClearing = isClearing,
                    IsError = isError,
                    ErrorDetails = errorDetails
                };

                await SendTextStatusAsync(status, sessionId);
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
