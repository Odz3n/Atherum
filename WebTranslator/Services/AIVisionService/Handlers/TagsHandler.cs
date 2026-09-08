using Azure.AI.Vision.ImageAnalysis;
using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.AIVisionService.Handlers
{
    public class TagsHandler : IAnalysisHandler
    {
        public AnalysisFeature Feature => AnalysisFeature.Tags;

        public Task ProcessAsync(
            ImageAnalysisResult azureResult,
            VisionAnalysisResult result,
            AnalysisOptions options)
        {
            if (azureResult?.Tags == null)
                return Task.CompletedTask;

            var tags = azureResult.Tags.Values.Select(tag => new VisionTag
            {
                Name = tag.Name,
                Confidence = tag.Confidence
            });

            if (options.MinConfidence > 0)
                tags = tags.Where(t => t.Confidence >= options.MinConfidence);

            if (options.MaxResults > 0)
                tags = tags.Take(options.MaxResults);

            tags = tags.OrderByDescending(t => t.Confidence);

            result.Tags.AddRange(tags);

            return Task.CompletedTask;
        }
    }
}
