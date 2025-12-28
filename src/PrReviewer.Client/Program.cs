using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrReviewer.Client;
using PrReviewer.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:57704/") 
});

builder.Services.AddScoped<IPrReviewService, PrReviewService>();

await builder.Build().RunAsync();

