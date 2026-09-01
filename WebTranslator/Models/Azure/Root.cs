namespace WebTranslator.Models.Azure
{
    public class Root
    {
        public DetectedLanguage? DetectedLanguage { get; set; }
        public List<Translation>? Translations { get; set; }
    }
}
