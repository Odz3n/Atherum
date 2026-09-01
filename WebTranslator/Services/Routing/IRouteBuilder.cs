namespace WebTranslator.Services.Routing
{
    public interface IRouteBuilder
    {
        string BuildTranslateRoute(string fromLanguage, List<string> toLanguages);
    }
}
