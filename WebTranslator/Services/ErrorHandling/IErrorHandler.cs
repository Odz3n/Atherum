using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Services.ErrorHandling
{
    public interface IErrorHandler
    {
        Task HandleErrorAsync(
            Exception ex,
            string text,
            string? sessionId,
            Func<MessageType, string, string?, string?, bool, Task> sendStatus,
            bool isImage = false);
    }
}
