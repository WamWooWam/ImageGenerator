using Discord;
using Discord.WebSocket;
using ImageGeneratorService.Bot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = Host.CreateApplicationBuilder();

builder.Configuration
    .AddIniFile("settings.ini", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables("CLIPPYBOT_")
    .AddCommandLine(args);

builder.Services.AddHttpClient("Discord");
builder.Services.AddHttpClient("ClippyService", (c) =>
{
    c.BaseAddress = new Uri(builder.Configuration["ClippyServiceUrl"]!);
});

builder.Services.AddSingleton(c => new DiscordSocketConfig() { GatewayIntents = GatewayIntents.AllUnprivileged });
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddHostedService<BotService>();

using var host = builder.Build();
await host.RunAsync();
