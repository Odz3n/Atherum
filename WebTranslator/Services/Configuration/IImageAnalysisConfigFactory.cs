using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Services.Configuration
{
    public interface IImageAnalysisConfigFactory
    {
        ImageAnalysisConfig CreateDefault();
        ImageAnalysisConfig CreateForTextOnly();
        ImageAnalysisConfig CreateForObjectsOnly();
        ImageAnalysisConfig CreateForFullAnalysis();
        ImageAnalysisConfig CreateFromTranslationConfig(TranslationConfig translationConfig);
        ImageAnalysisConfig CreateCustom(Action<ImageAnalysisConfig> configure);
    }
    public class ImageAnalysisConfigFactory : IImageAnalysisConfigFactory
    {
        public ImageAnalysisConfig CreateCustom(Action<ImageAnalysisConfig> configure)
        {
            throw new NotImplementedException();
        }

        public ImageAnalysisConfig CreateDefault()
        {
            return new ImageAnalysisConfig
            {
                LangFrom = "auto",
                LangsTo = new List<string> { "en" },
                Features = new VisionFeatures
                {
                    EnableOcr = true,
                    EnableObjectDetection = true,
                    EnableTags = true,
                    MinConfidence = 0.5,
                    MaxResults = 10
                },
                Options = new VisionOptions
                {
                    Language = "en",
                    DetailLevel = AnalysisDetailLevel.Standard,
                    MaxImageSize = 4096
                }
            };
        }

        public ImageAnalysisConfig CreateForFullAnalysis()
        {
            throw new NotImplementedException();
        }

        public ImageAnalysisConfig CreateForObjectsOnly()
        {
            throw new NotImplementedException();
        }

        public ImageAnalysisConfig CreateForTextOnly()
        {
            throw new NotImplementedException();
        }

        public ImageAnalysisConfig CreateFromTranslationConfig(TranslationConfig translationConfig)
        {
            throw new NotImplementedException();
        }
    }
}
