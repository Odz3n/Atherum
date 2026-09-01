namespace WebTranslator.Models.TranslationDTOs.ImageTranslation
{
    public class ImageTranslationRequest
    {
        public string? ImageData { get; set; }
        public string FromLanguage { get; set; } = "auto";
        public List<string> ToLanguages { get; set; } = new();
        public string? SessionId { get; set; }
    }
}
