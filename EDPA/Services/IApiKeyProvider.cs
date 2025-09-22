namespace EDPA.Services
{
    public interface IApiKeyProvider
    {
        string GetEdsmApiKey();
        bool IsApiConfigured { get; }
    }
}