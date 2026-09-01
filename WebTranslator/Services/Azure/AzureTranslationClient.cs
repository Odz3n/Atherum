using Newtonsoft.Json;
using System.Text;
using WebTranslator.Models.Azure;
using WebTranslator.Models.TranslationDTOs.Common;

namespace WebTranslator.Services.Azure
{
    public class AzureTranslationClient : IAzureTranslationClient
    {
        private readonly ILogger<AzureTranslationClient> _logger;
        private const int API_TIMEOUT_SECONDS = 30;

        public AzureTranslationClient(
            ILogger<AzureTranslationClient> logger)
        {
            _logger = logger;
        }
        public async Task<List<Root>?> TranslateAsync(
            string text,
            string endpoint, 
            string apiKey,
            string route,
            string location)
        {
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                client.Timeout = TimeSpan.FromSeconds(API_TIMEOUT_SECONDS);

                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                _logger.LogInformation($"Sending translation request to: {request.RequestUri}");
                _logger.LogInformation($"Request body: {requestBody}");

                HttpResponseMessage response = await client.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response status: {response.StatusCode}");
                _logger.LogInformation($"Response body: {result}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"API Error: {response.StatusCode} - {result}");
                    throw new HttpRequestException($"API Error: {response.StatusCode} - {result}");
                }

                var translations = JsonConvert.DeserializeObject<List<Root>>(result);

                if (translations != null)
                {
                    _logger.LogInformation($"Successfully deserialized {translations.Count} translation(s)");

                    foreach (var root in translations)
                    {
                        if (root.DetectedLanguage != null)
                        {
                            _logger.LogInformation($"Detected language: {root.DetectedLanguage.Language} (score: {root.DetectedLanguage.Score})");
                        }

                        if (root.Translations != null)
                        {
                            foreach (var translation in root.Translations)
                            {
                                _logger.LogInformation($"Translation to {translation.To}: {translation.Text}");
                            }
                        }
                    }
                }
                return translations;
            }
        }
    }
}
