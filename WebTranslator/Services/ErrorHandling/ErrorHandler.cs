using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Services.ErrorHandling
{
    public class ErrorHandler : IErrorHandler
    {
        private readonly ILogger<ErrorHandler> _logger;
        public ErrorHandler(
            ILogger<ErrorHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleErrorAsync(
            Exception ex,
            string text,
            string?
            sessionId,
            Func<MessageType, string, string?, string?, bool, Task> sendStatus,
            bool isImage = false)
        {
            var (type, message) = GetErrorTypeAndMessage(ex);
            _logger.LogError(ex, "{ErrorType}: {Message}", type, message);

            await sendStatus(type, message, text, sessionId, true);
        }

        private (MessageType Type, string Message) GetErrorTypeAndMessage(Exception ex)
        {
            return ex switch
            {
                HttpRequestException => (MessageType.NetworkError, $"Network error: {ex.Message}"),
                TaskCanceledException => (MessageType.TimeoutError, "Request timeout - please try again"),
                _ => (MessageType.TranslationFailed, $"Translation failed: {ex.Message}")
            };
        }
    }
}
