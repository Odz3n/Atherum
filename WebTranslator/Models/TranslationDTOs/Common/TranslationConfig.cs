namespace WebTranslator.Models.TranslationDTOs.Common
{
    public class TranslationConfig
    {
        public List<string> LangsTo { get; set; } = new();
        public string LangFrom { get; set; } = "auto";
        public string Location { get; set; } = "germanywestcentral";
    }
}
