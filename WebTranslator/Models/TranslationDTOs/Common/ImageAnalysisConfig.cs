using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Models.TranslationDTOs.Common
{
    public enum AnalysisDetailLevel
    {
        Basic,
        Standard,
        Advanced,
        Full
    }
    public class ImageAnalysisConfig
    {
        // Azure Translation
        public string LangFrom { get; set; } = "auto";
        public List<string> LangsTo { get; set; } = new();

        // Azure Vision
        public VisionFeatures Features { get; set; } = new();
        public VisionOptions Options { get; set; } = new();

        // Processing
        public ProcessingOptions Processing { get; set; } = new();
    }
    public class VisionFeatures
    {
        public bool EnableOcr { get; set; } = true;
        public bool EnableObjectDetection { get; set; } = true;
        public bool EnableTags { get; set; } = true;
        public double MinConfidence { get; set; } = 0.5;
        public int MaxResults { get; set; } = 10;
    }
    public class VisionOptions
    {
        public string Language { get; set; } = "en";
        public AnalysisDetailLevel DetailLevel { get; set; } = AnalysisDetailLevel.Standard;
        public int? MaxImageSize { get; set; } = 4096;
    }
}
