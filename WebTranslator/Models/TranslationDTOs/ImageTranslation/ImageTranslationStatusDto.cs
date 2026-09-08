using WebTranslator.Models.TranslationDTOs.TextTranslation;
using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Models.TranslationDTOs.ImageTranslation
{
    public class ImageTranslationStatusDto : TranslationStatusDto
    {
        public string? ExtractedText { get; set; }
        public int OcrProgress { get; set; }
        public double OcrConfidence { get; set; }
        public string? ImagePreview { get; set; }
        public List<VisionObject>? Objects { get; set; }
        public List<VisionTag>? Tags { get; set; }
        public string? BlobUri { get; set; }
        public List<TranslationDto>? Translations { get; set; }
        public int AnalysisProgress { get; set; }
        public int ObjectsCount => Objects?.Count ?? 0;
        public int TagsCount => Tags?.Count ?? 0;
    }
}
