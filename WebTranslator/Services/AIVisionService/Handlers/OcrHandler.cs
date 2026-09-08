using Azure.AI.Vision.ImageAnalysis;
using WebTranslator.Models.TranslationDTOs.Vision;

namespace WebTranslator.Services.AIVisionService.Handlers
{
    public class OcrHandler : IAnalysisHandler
    {
        public AnalysisFeature Feature => AnalysisFeature.Read;

        public Task ProcessAsync(
            ImageAnalysisResult azureResult,
            VisionAnalysisResult result,
            AnalysisOptions options)
        {
            if (azureResult?.Read == null)
                return Task.CompletedTask;

            var text = string.Join(" ", azureResult.Read.Blocks
                .SelectMany(b => b.Lines)
                .Select(l => l.Text));

            result.ExtractedText = text;

            if (options.IncludeBoundingBoxes)
            {
                var imageWidth = azureResult.Metadata.Width;
                var imageHeight = azureResult.Metadata.Height;

                var words = azureResult.Read.Blocks
                    .SelectMany(b => b.Lines)
                    .SelectMany(l => l.Words)
                    .Select(w =>
                    {
                        var minX = w.BoundingPolygon.Min(p => p.X);
                        var minY = w.BoundingPolygon.Min(p => p.Y);
                        var maxX = w.BoundingPolygon.Max(p => p.X);
                        var maxY = w.BoundingPolygon.Max(p => p.Y);

                        return new OcrWord
                        {
                            Text = w.Text,
                            Confidence = w.Confidence,
                            BoundingBox = new BoundingBox
                            {
                                X = imageWidth > 0 ? minX / (double)imageWidth * 100 : 0,
                                Y = imageHeight > 0 ? minY / (double)imageHeight * 100 : 0,
                                Width = imageWidth > 0 ? (maxX - minX) / (double)imageWidth * 100 : 0,
                                Height = imageHeight > 0 ? (maxY - minY) / (double)imageHeight * 100 : 0
                            }
                        };
                    }).ToList();

                result.Words = words;
            }

            return Task.CompletedTask;
        }
    }
}