using Discord.Interactions;
using Discord;
using Discord.WebSocket;
using ImageGeneratorService.Bot.Interactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImageGeneratorService.Bot;

internal class BotService(
    IConfiguration configuration,
    IServiceProvider services,
    ILoggerFactory loggerFactory,
    DiscordSocketClient client) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<DiscordSocketClient>();

        var interactionService = new InteractionService(client.Rest, new InteractionServiceConfig() { LogLevel = LogSeverity.Debug });

        interactionService.Log += (msg) =>
        {
            logger.LogInformation(msg.Message);
            return Task.CompletedTask;
        };

        client.Log += (msg) =>
        {
            logger.LogInformation(msg.Message);
            return Task.CompletedTask;
        };

        client.InteractionCreated += async (interaction) =>
        {
            var ctx = new SocketInteractionContext(client, interaction);
            await interactionService.ExecuteCommandAsync(ctx, services);
        };

        client.Ready += async () =>
        {
            try
            {
                await interactionService.AddModuleAsync<ClippyInteractions>(services);
                await interactionService.RegisterCommandsGloballyAsync();

                logger.LogInformation("Got {N} commands!", interactionService.SlashCommands.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in READY!");
            }
        };

        var token = configuration["Token"];

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.StopAsync();
    }
}
