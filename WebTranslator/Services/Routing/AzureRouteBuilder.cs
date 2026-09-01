
namespace WebTranslator.Services.Routing
{
    public class AzureRouteBuilder : IRouteBuilder
    {
        private const string API_VERSION = "3.0";
        private const string BASE_PATH = "/translate";
        public string BuildTranslateRoute(string fromLanguage, List<string> toLanguages)
        {
            if (toLanguages == null || !toLanguages.Any())
                throw new ArgumentException("At least one target language is required", nameof(toLanguages));

            var route = $"{BASE_PATH}?api-version={API_VERSION}";

            if (!string.IsNullOrEmpty(fromLanguage) && fromLanguage != "auto")
                route += $"&from={fromLanguage}";

            route += $"&{string.Join("&", toLanguages.Select(x => $"to={x}"))}";

            return route;
        }
    }
}
