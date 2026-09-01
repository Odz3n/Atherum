using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Services.Validation
{
    public class ImageValidator : IImageValidator
    {
        private readonly ILogger<ImageValidator> _logger;

        private const int MAX_IMAGE_SIZE_MB = 10;
        private const int MAX_IMAGE_SIZE_BYTES = MAX_IMAGE_SIZE_MB * 1024 * 1024;
        
        public ImageValidator(
            ILogger<ImageValidator> logger)
        {
            _logger = logger;
        }
        public ImageValidationResult Validate(string imageData)
        {
            if (string.IsNullOrEmpty(imageData))
            {
                return new ImageValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "No image provided",
                    ErrorType = MessageType.ValidationError
                };
            }

            try
            {
                var base64Data = imageData.Contains(",")
                    ? imageData.Split(',')[1]
                    : imageData;

                var imageBytes = Convert.FromBase64String(base64Data);

                return Validate(imageBytes);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Invalid image format");
                return new ImageValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid image format",
                    ErrorType = MessageType.ValidationError
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image validation failed");
                return new ImageValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Image validation failed: {ex.Message}",
                    ErrorType = MessageType.ValidationError
                };
            }
        }

        public ImageValidationResult Validate(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return new ImageValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Image is empty",
                    ErrorType = MessageType.ValidationError
                };
            }

            if (imageBytes.Length > MAX_IMAGE_SIZE_BYTES)
            {
                return new ImageValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Image too large (max {MAX_IMAGE_SIZE_MB}MB, actual: {(imageBytes.Length / 1024.0 / 1024.0):F1}MB)",
                    ErrorType = MessageType.ValidationError,
                    ImageBytes = imageBytes,
                    ImageSize = imageBytes.Length
                };
            }

            return new ImageValidationResult
            {
                IsValid = true,
                ImageBytes = imageBytes,
                ImageSize = imageBytes.Length
            };
        }
    }
}
