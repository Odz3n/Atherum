namespace WebTranslator.Models.TranslationDTOs.Vision
{
    public class VisionObject
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public BoundingBox BoundingBox { get; set; } = new();
        public string? TranslatedName { get; set; }
    }
}
