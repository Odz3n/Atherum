using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.ImageTranslation;
using WebTranslator.Models.TranslationDTOs.TextTranslation;

namespace WebTranslator.Services.Status
{
    public interface IStatusSender
    {
        Task SendTextStatusAsync(
            TranslationStatusDto status,
            string? sessionId = null);

        Task SendImageStatusAsync(
            ImageTranslationStatusDto status,
            string? sessionId = null);

        Task SendStatusAsync(
            // general params
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
            // Image specific params
            int ocrProgress = 0,
            double ocrConfidence = 0,
            string? imagePreview = null,
            string? extractedText = null,
            bool isImage = false);
    }
}
