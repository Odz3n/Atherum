using Azure.AI.Vision.ImageAnalysis;
using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.AIVisionService.Handlers
{
    public class ObjectsHandler : IAnalysisHandler
    {
        public AnalysisFeature Feature => AnalysisFeature.Objects;

        public Task ProcessAsync(
            ImageAnalysisResult azureResult,
            VisionAnalysisResult result,
            AnalysisOptions options)
        {
            if (azureResult?.Objects == null)
                return Task.CompletedTask;

            var imageWidth = azureResult.Metadata.Width;
            var imageHeight = azureResult.Metadata.Height;

            var objects = azureResult.Objects.Values
                .Select(obj =>
                {
                    var box = obj.BoundingBox;

                    return new VisionObject
                    {
                        Name = obj.Tags.FirstOrDefault()?.Name ?? "Unknown",
                        Confidence = obj.Tags.FirstOrDefault()?.Confidence ?? 0,
                        BoundingBox = new BoundingBox
                        {
                            X = imageWidth > 0 ? box.X / (double)imageWidth * 100 : 0,
                            Y = imageHeight > 0 ? box.Y / (double)imageHeight * 100 : 0,
                            Width = imageWidth > 0 ? box.Width / (double)imageWidth * 100 : 0,
                            Height = imageHeight > 0 ? box.Height / (double)imageHeight * 100 : 0
                        }
                    };
                });

            if (options.MinConfidence > 0)
                objects = objects.Where(obj => obj.Confidence >= options.MinConfidence);

            if (options.MaxResults > 0)
                objects = objects.Take(options.MaxResults);

            objects = objects.OrderByDescending(obj => obj.Confidence);

            result.Objects.AddRange(objects);

            return Task.CompletedTask;
        }
    }
}