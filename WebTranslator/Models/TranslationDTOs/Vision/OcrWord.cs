namespace WebTranslator.Models.TranslationDTOs.Vision
{
    public class OcrWord
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public BoundingBox BoundingBox { get; set; } = new();
    }
}
