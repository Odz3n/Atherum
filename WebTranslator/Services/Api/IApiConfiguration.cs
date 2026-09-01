namespace WebTranslator.Services.Api
{
    public interface IApiConfiguration<T>
    {
        T? GetConfig();
    }
}
