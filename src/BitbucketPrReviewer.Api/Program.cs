using BitbucketPrReviewer.Api.Services;
using BitbucketPrReviewer.Api.Settings;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<BitbucketSettings>(builder.Configuration.GetSection("Bitbucket"));
builder.Services.Configure<GitHubSettings>(builder.Configuration.GetSection("GitHub"));
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<BitbucketClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<BitbucketSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
    {
        client.BaseAddress = new Uri(settings.BaseUrl);
    }
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});

builder.Services.AddHttpClient<GitHubClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<GitHubSettings>>().Value;
    
    if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
    {
        client.BaseAddress = new Uri(settings.BaseUrl);
    }
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Allow envs with corporate proxies/SSL interceptors if needed later
});

builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddSingleton<OpenAIClient>();
builder.Services.AddScoped<ReviewService>();

var app = builder.Build();

app.MapControllers();

app.Run();


