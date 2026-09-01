using WebTranslator.Models.Azure;

namespace WebTranslator.Services.Azure
{
    public interface IAzureTranslationClient
    {
        Task<List<Root>?> TranslateAsync(
            string text,
            string endpoint,
            string apiKey,
            string route,
            string location);
    }
}
