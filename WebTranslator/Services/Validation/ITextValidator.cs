using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Services.Validation
{
    public interface ITextValidator
    {
        ValidationResultTest Validate(string text);
    }
    public class ValidationResultTest
    {
        public bool IsValid { get; set; }
        public MessageType Type { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
