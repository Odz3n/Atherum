namespace WebTranslator.Models.TranslationDTOs.Vision
{
    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<OcrWord> Words { get; set; } = new();
        public string? DetectedLanguage { get; set; }
        public int WordCount { get; set; }
    }
}
