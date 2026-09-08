using Azure.AI.Vision.ImageAnalysis;
using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.AIVisionService
{
    public interface IAnalysisHandler
    {
        AnalysisFeature Feature { get; }
        Task ProcessAsync(
            ImageAnalysisResult azureResult,
            VisionAnalysisResult result,
            AnalysisOptions options);
    }
}
