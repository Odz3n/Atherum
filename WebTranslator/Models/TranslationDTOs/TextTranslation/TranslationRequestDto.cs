namespace WebTranslator.Models.TranslationDTOs.TextTranslation
{
    public class TranslationRequestDto
    {
        public string Text { get; set; } = string.Empty;
        public string FromLanguage { get; set; } = "auto";
        public List<string> ToLanguages { get; set; } = new();
        public string? SessionId { get; set; }
        public string? Action { get; set; }
    }
}
