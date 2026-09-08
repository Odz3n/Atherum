using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.AIVisionService
{
    public interface IVisionService
    {
        Task<OcrResult> ExtractTextAsync(byte[] imageData);
        Task<OcrResult> ExtractTextFromUrlAsync(string imageUrl);
        Task<VisionAnalysisResult> AnalyzeImageAsync(
            byte[] imageData,
            AnalysisOptions? options = null);
        Task<VisionAnalysisResult> AnalyzeImageFromUrlAsync(
            string imageUrl,
            AnalysisOptions? options = null);
    }
}
