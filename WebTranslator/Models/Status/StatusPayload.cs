using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.TextTranslation;

namespace WebTranslator.Models.Status
{
    public record StatusPayload(
        MessageType Type,
        string Message,
        string? OriginalText = null,
        string? SessionId = null,
        string? TranslatedText = null,
        string? DetectedLanguage = null,
        double? DetectedLanguageScore = null,
        List<string>? TargetLanguages = null,
        List<TranslationDto>? AllTranslations = null,
        double? Progress = null,
        bool IsClearing = false,
        bool IsError = false,
        string? ErrorDetails = null,
        int OcrProgress = 0,
        double OcrConfidence = 0,
        string? ImagePreview = null,
        string? ExtractedText = null,
        bool IsImage = false
    );
}
