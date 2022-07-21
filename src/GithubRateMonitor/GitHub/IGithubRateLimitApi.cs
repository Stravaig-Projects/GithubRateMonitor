using Refit;

namespace GithubRateMonitor;

public interface IGithubRateLimitApi
{
    [Get("/rate_limit")]
    public Task<RateLimitResponseDto> GetRateLimitAsync(CancellationToken ct);
}

public class RateLimitResponseDto
{
    public ResourcesDto Resources { get; set; }    
}

public class ResourcesDto
{
    public RateLimitDto Core { get; set; }
    public RateLimitDto Search { get; set; }
    public RateLimitDto GraphQl { get; set; }
    public RateLimitDto IntegrationManifest { get; set; }
}

public class RateLimitDto
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public long Reset { get; set; }
    public int Used { get; set; }
}