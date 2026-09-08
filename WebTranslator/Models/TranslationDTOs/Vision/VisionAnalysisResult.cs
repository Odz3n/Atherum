using WebTranslator.Models.TranslationDTOs.TextTranslation;

namespace WebTranslator.Models.TranslationDTOs.Vision
{
    public class VisionAnalysisResult
    {
        public string ExtractedText { get; set; } = string.Empty;
        public string? DetectedLanguage { get; set; }
        public List<VisionObject> Objects { get; set; } = new();
        public List<VisionTag> Tags { get; set; } = new();
        public List<TranslationDto> Translations { get; set; } = new();
        public List<OcrWord> Words { get; set; } = new();
        public string? BlobUri { get; set; }
        public string? ImagePreview { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
