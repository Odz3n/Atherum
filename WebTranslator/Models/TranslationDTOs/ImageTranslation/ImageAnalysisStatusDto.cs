using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Models.TranslationDTOs.TextTranslation;
using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Models.TranslationDTOs.ImageTranslation
{
    public class ImageAnalysisStatusDto
    {
        public MessageType Type { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsError { get; set; }
        public string? ErrorDetails { get; set; }

        public int Progress { get; set; }

        public string? ExtractedText { get; set; }
        public string? DetectedLanguage { get; set; }

        public List<VisionObject> Objects { get; set; } = new();
        public List<VisionTag> Tags { get; set; } = new();
        public List<TranslationDto> Translations { get; set; } = new();

        public string? BlobUri { get; set; }
        public string? ImagePreview { get; set; }

        public int ObjectsCount => Objects?.Count ?? 0;
        public int TagsCount => Tags?.Count ?? 0;
        public bool HasObjects => Objects != null && Objects.Any();
        public bool HasTags => Tags != null && Tags.Any();
        public bool HasTranslations => Translations != null && Translations.Any();
        public bool HasText => !string.IsNullOrEmpty(ExtractedText);
    }
}