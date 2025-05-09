public interface IApiDiscoveryService
{
    string ApiBaseUrl { get; }
    Task<string> DiscoverApiUrl();
}