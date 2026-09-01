using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebTranslator.Models;
using WebTranslator.Models.TranslationDTOs;
using WebTranslator.Services.TranslationService;

namespace WebTranslator.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public HomeController(
            ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        //[HttpPost]
        //public async Task<IActionResult> Translate(
        //    [FromBody] TranslationRequestDto request)
        //{
        //    try
        //    {
        //        _translationService.SetConfig(new TranslationConfig
        //        {
        //            Location = "germanywestcentral",
        //            LangFrom = request.FromLanguage,
        //            LangsTo = request.ToLanguages
        //        });

        //        await _translationService.ProcessTextAsync(request.Text);
        //        return Ok(new { status = "success", message = "Translation processed" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Translation failed for text: {request.Text}");
        //        return StatusCode(500, new { error = "Translation failed", details = ex.Message });
        //    }
        //}

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
