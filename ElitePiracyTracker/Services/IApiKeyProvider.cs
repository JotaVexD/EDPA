namespace ElitePiracyTracker.Services
{
    public interface IApiKeyProvider
    {
        string GetEdsmApiKey();
        bool IsApiConfigured { get; }
    }
}