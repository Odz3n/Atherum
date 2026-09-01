using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Services.Validation
{
    public interface IImageValidator
    {
        ImageValidationResult Validate(string imageData);
        ImageValidationResult Validate(byte[] imageBytes);
    }
    public class ImageValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public MessageType ErrorType { get; set; }
        public byte[]? ImageBytes { get; set; }
        public long ImageSize { get; set; }
    }
}
