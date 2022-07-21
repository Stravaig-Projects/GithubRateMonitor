using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;

namespace GithubRateMonitor;

public class ApplicationHost : BackgroundService
{
    private readonly IGithubRateLimitApi _api;
    private readonly ApplicationSettings _settings;

    public ApplicationHost(IGithubRateLimitApi api, IOptions<ApplicationSettings> settings)
    {
        _api = api;
        _settings = settings.Value;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.Clear();
        while (!stoppingToken.IsCancellationRequested)
        {
            var data = await GetRateLimitAsync(stoppingToken);
            WriteData(data);
            
            await Task.Delay(_settings.PollingIntervalSeconds * 1000, stoppingToken);
        }
    }

    private async Task<RateLimitResponseDto> GetRateLimitAsync(CancellationToken stoppingToken)
    {
        try
        {
            return await _api.GetRateLimitAsync(stoppingToken);
        }
        catch (ApiException apiEx)
        {
            Console.WriteLine(apiEx);
            Console.WriteLine();
            Console.WriteLine($"{apiEx.RequestMessage.Method} {apiEx.RequestMessage.RequestUri}");
            Console.WriteLine($"{(int)apiEx.StatusCode} {apiEx.ReasonPhrase}");
            foreach (var header in apiEx.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(";", header.Value)}");
            }
            Console.WriteLine();
            Console.WriteLine(apiEx.Content);
            Console.WriteLine();
            throw;
        }
    }

    private int _width;
    private int _height;
    private void WriteData(RateLimitResponseDto data)
    {
        _width = Console.WindowWidth;
        _height = Console.WindowHeight;
        Console.SetCursorPosition(0,0);
        SetStandardColour();

        var msg = $"GitHub API Rate Limit";
        var time = DateTime.Now.ToString("F");
        var padLength = _width - msg.Length - time.Length;
        if (padLength < 2)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("** Increase the window width! **");
            SetStandardColour();
        }
        var pad = new String(' ', padLength);
        Console.WriteLine($"{msg}{pad}{time}");
        Console.WriteLine(new string('-', _width));
        
        WriteSection(data.Resources.Core, nameof(data.Resources.Core));
        WriteSection(data.Resources.Search, nameof(data.Resources.Search));
        WriteSection(data.Resources.GraphQl, "GraphQL");
        WriteSection(data.Resources.IntegrationManifest, "Integration Manifest");
    }

    private static void SetStandardColour()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    private void WriteSection(RateLimitDto? rate, string name)
    {
        if (rate == null)
            return;
        string line = new string(' ', _width);
        Console.WriteLine(line);
        var resetTime = DateTime.UnixEpoch.AddSeconds(rate.Reset).ToLocalTime();
        var time = $"Resets at {resetTime:T}";
        var padLength = _width - time.Length - name.Length;
        if (padLength < 2)
            padLength = 2;
        var pad = new string(' ', padLength);
        string msg = $"{name}{pad}{time}";
        Console.WriteLine(msg);

        var usedCharCount = (rate.Used * (_width - 2)) / rate.Limit;
        var used = new string('#', usedCharCount);
        var remainingCharCount = _width - 2 - usedCharCount;
        var remaining = new string('-', remainingCharCount);
        Console.WriteLine(new string('*', _width));
        Console.Write("*");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(used);
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write(remaining);
        SetStandardColour();
        Console.WriteLine("*");
        Console.WriteLine(new string('*', _width));

        var waitUntilReset = resetTime - DateTime.Now;
        var minutes = (int)waitUntilReset.TotalMinutes;
        var seconds = (int) waitUntilReset.TotalSeconds - (minutes * 60);
        var summaryMsg = $"Used {rate.Used} of {rate.Limit}, {rate.Remaining} remaining for {minutes}m {seconds}s.";
        var summaryPadChars = _width - summaryMsg.Length;
        if (summaryPadChars < 1)
            summaryPadChars = 1;
        Console.WriteLine(summaryMsg+new string(' ', summaryPadChars));
    }
}