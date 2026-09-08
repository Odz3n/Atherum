using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Models.TranslationDTOs.ImageTranslation
{
    public class ImageAnalysisRequest
    {
        public string ImageData { get; set; } = string.Empty;
        public string FromLanguage { get; set; } = "auto";
        public List<string> ToLanguages { get; set; } = new();
        public AnalysisOptions? Options { get; set; }
        public string? SessionId { get; set; }
    }
}
