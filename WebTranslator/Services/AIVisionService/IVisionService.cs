using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.AIVisionService
{
    public interface IVisionService
    {
        Task<OcrResult> ExtractTextAsync(byte[] imageData);
        Task<OcrResult> ExtractTextFromUrlAsync(string imageUrl);
    }
}
