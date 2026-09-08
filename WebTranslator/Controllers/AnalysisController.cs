using Microsoft.AspNetCore.Mvc;
using WebTranslator.Services.TranslationService;

namespace WebTranslator.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly ILogger<AnalysisController> _logger;
        private readonly TranslationService _translationService;

        public AnalysisController(
            ILogger<AnalysisController> logger,
            TranslationService translationService)
        {
            _logger = logger;
            _translationService = translationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}