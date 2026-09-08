using WebTranslator.Models.Status;
using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.ImageTranslation;
using WebTranslator.Models.TranslationDTOs.TextTranslation;
using WebTranslator.Models.TranslationDTOs.Vision;

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
            StatusPayload payload);

        Task SendAnalysisStatusAsync(
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
            bool isError = false);
    }
}
