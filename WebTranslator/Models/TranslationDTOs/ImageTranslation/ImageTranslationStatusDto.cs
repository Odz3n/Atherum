using WebTranslator.Models.TranslationDTOs.TextTranslation;

namespace WebTranslator.Models.TranslationDTOs.ImageTranslation
{
    public class ImageTranslationStatusDto : TranslationStatusDto
    {
        public string? ExtractedText { get; set; }
        public int OcrProgress { get; set; }
        public double OcrConfidence { get; set; }
        public string? ImagePreview { get; set; }
    }
}
