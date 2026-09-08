using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Models.TranslationDTOs.Vision
{
    [Flags]
    public enum AnalysisFeature
    {
        None = 0,
        Read = 1 << 0,
        Objects = 1 << 1,
        Tags = 1 << 2
    }

    public class ProcessingOptions
    {
        public bool TranslateExtractedText { get; set; } = true;
        public bool TranslateObjectNames { get; set; } = true;
        public bool TranslateTagNames { get; set; } = true;
        public bool SaveToBlob { get; set; }
        public bool? ReturnDownloadUrl { get; set; } = true;

        public string? LangFrom { get; set; }
        public List<string>? LangsTo { get; set; }
    }

    public class AnalysisOptions
    {
        public AnalysisFeature Features { get; set; } =
            AnalysisFeature.Read | AnalysisFeature.Objects | AnalysisFeature.Tags;

        public double MinConfidence { get; set; } = 0.5;
        public int MaxResults { get; set; } = 50;
        public bool IncludeBoundingBoxes { get; set; } = true;

        public ProcessingOptions? Processing { get; set; }

        public static AnalysisOptions Default() => new();

        public static AnalysisOptions TextOnly() => new()
        {
            Features = AnalysisFeature.Read,
            MinConfidence = 0.5,
            MaxResults = 50,
            IncludeBoundingBoxes = false,
            Processing = new ProcessingOptions
            {
                TranslateExtractedText = true,
                TranslateObjectNames = false,
                TranslateTagNames = false
            }
        };

        public static AnalysisOptions ObjectsOnly() => new()
        {
            Features = AnalysisFeature.Objects | AnalysisFeature.Tags,
            MinConfidence = 0.6,
            MaxResults = 30,
            IncludeBoundingBoxes = true,
            Processing = new ProcessingOptions
            {
                TranslateExtractedText = false,
                TranslateObjectNames = true,
                TranslateTagNames = true
            }
        };

        public static AnalysisOptions AllFeatures() => new()
        {
            Features = AnalysisFeature.Read | AnalysisFeature.Objects | AnalysisFeature.Tags,
            MinConfidence = 0.5,
            MaxResults = 50,
            IncludeBoundingBoxes = true,
            Processing = new ProcessingOptions
            {
                TranslateExtractedText = true,
                TranslateObjectNames = true,
                TranslateTagNames = true
            }
        };

        public static AnalysisOptions AllFeatures(
            string fromLanguage,
            List<string> toLanguages)
        {
            return new AnalysisOptions
            {
                Features = AnalysisFeature.Read | AnalysisFeature.Objects | AnalysisFeature.Tags,
                MinConfidence = 0.5,
                MaxResults = 50,
                IncludeBoundingBoxes = true,
                Processing = new ProcessingOptions
                {
                    TranslateExtractedText = true,
                    TranslateObjectNames = true,
                    TranslateTagNames = true,
                    LangFrom = fromLanguage,
                    LangsTo = toLanguages
                }
            };
        }

        public static AnalysisOptions TextOnly(
            string fromLanguage,
            List<string> toLanguages)
        {
            return new AnalysisOptions
            {
                Features = AnalysisFeature.Read,
                MinConfidence = 0.5,
                MaxResults = 50,
                IncludeBoundingBoxes = false,
                Processing = new ProcessingOptions
                {
                    TranslateExtractedText = true,
                    TranslateObjectNames = false,
                    TranslateTagNames = false,
                    LangFrom = fromLanguage,
                    LangsTo = toLanguages
                }
            };
        }

        public static AnalysisOptions ObjectsOnly(
            string fromLanguage,
            List<string> toLanguages)
        {
            return new AnalysisOptions
            {
                Features = AnalysisFeature.Objects | AnalysisFeature.Tags,
                MinConfidence = 0.6,
                MaxResults = 30,
                IncludeBoundingBoxes = true,
                Processing = new ProcessingOptions
                {
                    TranslateExtractedText = false,
                    TranslateObjectNames = true,
                    TranslateTagNames = true,
                    LangFrom = fromLanguage,
                    LangsTo = toLanguages
                }
            };
        }

        public static AnalysisOptions Create(
            AnalysisFeature features,
            string fromLanguage,
            List<string> toLanguages,
            double minConfidence = 0.5,
            int maxResults = 50,
            bool includeBoundingBoxes = true,
            bool translateText = true,
            bool translateObjects = true,
            bool translateTags = true)
        {
            return new AnalysisOptions
            {
                Features = features,
                MinConfidence = minConfidence,
                MaxResults = maxResults,
                IncludeBoundingBoxes = includeBoundingBoxes,
                Processing = new ProcessingOptions
                {
                    TranslateExtractedText = translateText,
                    TranslateObjectNames = translateObjects,
                    TranslateTagNames = translateTags,
                    LangFrom = fromLanguage,
                    LangsTo = toLanguages
                }
            };
        }
        public static AnalysisOptions FromTranslationConfig(TranslationConfig config)
        {
            if (config == null)
                return AllFeatures();

            return new AnalysisOptions
            {
                Features = AnalysisFeature.Read | AnalysisFeature.Objects | AnalysisFeature.Tags,
                MinConfidence = 0.5,
                MaxResults = 50,
                IncludeBoundingBoxes = true,
                Processing = new ProcessingOptions
                {
                    TranslateExtractedText = true,
                    TranslateObjectNames = true,
                    TranslateTagNames = true,
                    LangFrom = config.LangFrom,
                    LangsTo = config.LangsTo,
                }
            };
        }
    }
}