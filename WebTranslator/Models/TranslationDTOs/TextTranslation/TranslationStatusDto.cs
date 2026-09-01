using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Models.TranslationDTOs.TextTranslation
{
    public class TranslationStatusDto
    {
        public MessageType Type { get; set; }
        public string? Message { get; set; }
        public string? OriginalText { get; set; }
        public string? TranslatedText { get; set; }
        public string? DetectedLanguage { get; set; }
        public double? DetectedLanguageScore { get; set; }
        public List<string>? TargetLanguages { get; set; }
        public List<TranslationDto>? AllTranslations { get; set; }
        public double? Progress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsClearing { get; set; }
        public bool IsError { get; set; }
        public string? ErrorDetails { get; set; }
    }
}
