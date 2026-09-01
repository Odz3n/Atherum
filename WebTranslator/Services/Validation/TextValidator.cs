using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Services.Validation
{
    public class TextValidator : ITextValidator
    {
        private const int MIN_TEXT_LENGTH = 2;
        private const int MAX_TEXT_LENGTH = 5000;
        public ValidationResultTest Validate(string text)
        {
            if (text == null)
                return new ValidationResultTest
                {
                    IsValid = false,
                    Type = MessageType.ValidationError,
                    Message = "Text cannot be null"
                };

            if (string.IsNullOrWhiteSpace(text))
                return new ValidationResultTest
                {
                    IsValid = false,
                    Type = MessageType.Empty,
                    Message = "Please enter text to translate"
                };

            if (text.Length < MIN_TEXT_LENGTH)
                return new ValidationResultTest
                {
                    IsValid = false,
                    Type = MessageType.TooShort,
                    Message = $"Please enter at least {MIN_TEXT_LENGTH} characters"
                };

            if (text.Length > MAX_TEXT_LENGTH)
                return new ValidationResultTest
                {
                    IsValid = false,
                    Type = MessageType.TooLong,
                    Message = $"Text exceeds {MAX_TEXT_LENGTH} characters (current: {text.Length})"
                };

            if (text.Any(char.IsControl) && !text.All(char.IsControl))
                return new ValidationResultTest
                {
                    IsValid = false,
                    Type = MessageType.InvalidCharacters,
                    Message = "Text contains invalid characters"
                };

            return new ValidationResultTest
            {
                IsValid = true,
                Type = MessageType.Valid,
                Message = "Text is valid"
            };
        }
    }
}
