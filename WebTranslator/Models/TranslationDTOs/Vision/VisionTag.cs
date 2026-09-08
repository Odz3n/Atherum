namespace WebTranslator.Models.TranslationDTOs.Vision
{
    public class VisionTag
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string? TranslatedName { get; set; }
    }
}
