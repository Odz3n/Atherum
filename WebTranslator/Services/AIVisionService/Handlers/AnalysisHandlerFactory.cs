using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.AIVisionService.Handlers
{
    public class AnalysisHandlerFactory : IAnalysisHandlerFactory
    {
        private readonly Dictionary<AnalysisFeature, IAnalysisHandler> _handlers;
        public AnalysisHandlerFactory(
            IEnumerable<IAnalysisHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.Feature);
        }
        public IEnumerable<IAnalysisHandler> GetHandlers(AnalysisFeature features)
        {
            if (features.HasFlag(AnalysisFeature.Read)
                && _handlers.TryGetValue(AnalysisFeature.Read, out var readHandler))
                yield return readHandler;
            
            if (features.HasFlag(AnalysisFeature.Objects) 
                && _handlers.TryGetValue(AnalysisFeature.Objects, out var objectsHandler))
                yield return objectsHandler;

            if (features.HasFlag(AnalysisFeature.Tags)
                && _handlers.TryGetValue(AnalysisFeature.Tags, out var tagsHandler))
                yield return tagsHandler;
        }
    }
}
