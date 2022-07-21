namespace GithubRateMonitor;

public class GitHubApiSettings
{
    public string BaseUrl { get; set; } = "https://api.github.com";
    public string Token { get; set; }
    
    public string UserName { get; set; }
}