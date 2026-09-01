using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebTranslator.Hubs;
using WebTranslator.Models.TranslationDTOs;
using WebTranslator.Services.TranslationService;

namespace WebTranslator.Controllers
{
    public class OcrController : Controller
    {
        private readonly ILogger<OcrController> _logger;
        private readonly TranslationService _translationService;
        private readonly IHubContext<TranslationHub> _hubContext;

        public OcrController(
            ILogger<OcrController> logger,
            TranslationService translationService,
            IHubContext<TranslationHub> hubContext)
        {
            _logger = logger;
            _translationService = translationService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(
            IFormFile image,
            string fromLanguage,
            List<string> toLanguages,
            string? sessionId = null)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { error = "Please select an image" });

            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(image.ContentType))
                return BadRequest(new { error = "Please upload a valid image (JPEG, PNG)" });

            if (image.Length > 10 * 1024 * 1024)
                return BadRequest(new { error = "Image too large (max 10MB)" });

            try
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var base64Image = Convert.ToBase64String(imageBytes);
                var imageData = $"data:{image.ContentType};base64,{base64Image}";

                _translationService.SetConfig(new Models.TranslationDTOs.Common.TranslationConfig
                {
                    LangFrom = fromLanguage ?? "auto",
                    LangsTo = toLanguages ?? new List<string> { "en" },
                    Location = "germanywestcentral"
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _translationService.ProcessImageAsync(imageData, sessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background image processing failed");
                        await _hubContext.Clients.Client(sessionId ?? "all").SendAsync("TranslationError", new
                        {
                            message = ex.Message,
                            type = "processing_error",
                            timestamp = DateTime.UtcNow
                        });
                    }
                });

                return Ok(new
                {
                    success = true,
                    message = "Image uploaded successfully! Processing...",
                    sessionId = sessionId ?? "all",
                    imagePreview = imageData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}