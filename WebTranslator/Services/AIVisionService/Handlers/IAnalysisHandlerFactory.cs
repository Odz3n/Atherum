using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.AIVisionService.Handlers
{
    public interface IAnalysisHandlerFactory
    {
        IEnumerable<IAnalysisHandler> GetHandlers(AnalysisFeature features);
    }
}
