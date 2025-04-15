using System.Globalization;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;

namespace ImageGeneratorService.Bot.Interactions;

[CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
public class ClippyInteractions(IHttpClientFactory httpClientFactory, IConfiguration configuration) : InteractionModuleBase
{
    public enum ClippyCharacter
    {
        [ChoiceDisplay("Clippit")]
        Clippy,
        [ChoiceDisplay("The Dot")]
        Dot,
        [ChoiceDisplay("F1")]
        HoverBot,
        [ChoiceDisplay("Mother Earth")]
        Nature,
        [ChoiceDisplay("Office Logo")]
        Office,
        [ChoiceDisplay("Rocky")]
        PowerPup,
        [ChoiceDisplay("Links")]
        Scribble,
        [ChoiceDisplay("Merlin")]
        Wizard,
        [ChoiceDisplay("Rover")]
        Rover,
        [ChoiceDisplay("The Genius")]
        Einstein,
        [ChoiceDisplay("BonziBUDDY")]
        Bonzi
    }

    public enum ClippyFont
    {
        [ChoiceDisplay("Tahoma")]
        Tahoma,
        [ChoiceDisplay("Comic Sans MS")]
        ComicSans,
        [ChoiceDisplay("Microsoft Sans Serif")]
        MSSansSerif,
        [ChoiceDisplay("Times New Roman")]
        Times,
        [ChoiceDisplay("Courier New")]
        CourierNew,
        [ChoiceDisplay("MS Gothic (Japan)")]
        MSGothic,
        [ChoiceDisplay("Papyrus")]
        Papyrus,
        [ChoiceDisplay("Arial")]
        Arial,
        [ChoiceDisplay("Arial Black")]
        ArialBlack,
        [ChoiceDisplay("Andalé Mono")]
        AndaleMono,
        [ChoiceDisplay("Georgia")]
        Georgia,
        [ChoiceDisplay("Impact")]
        Impact,
        [ChoiceDisplay("Trebuchet MS")]
        TrebuchetMS,
        [ChoiceDisplay("Verdana")]
        Verdana,
        [ChoiceDisplay("Wingdings")]
        Wingdings,
        [ChoiceDisplay("Webdings")]
        Webdings,
        [ChoiceDisplay("Elephant")]
        Elephant,
        [ChoiceDisplay("Calibri")]
        Calibri,
        [ChoiceDisplay("Cambria")]
        Cambria,
        [ChoiceDisplay("Segoe UI")]
        SegoeUI,
        [ChoiceDisplay("Nokia Sans")]
        Nokia,
        [ChoiceDisplay("Nokia Pure")]
        NokiaPure
    }

    private static readonly string[] CLIPPY_DISPLAY_NAMES
        = ["Clippit", "The Dot", "F1", "Mother Nature", "Office Logo", "Rocky", "Links", "Merlin", "Rover", "The Genius", "BonziBUDDY"];

    private static readonly string[] CLIPPY_DISPLAY_DESCRIPTIONS
        = ["A paperclip on a sheet of paper",
           "A red orb with a face",
           "A futuristic robot",
           "The globe",
           "4 puzzle pieces, interlocking into a square, red then blue then green then yellow.",
           "A cartoon dog, lightly golden fur, sat down and smiling",
           "A cartoon cat, lightly golden fir, sat down and smiling",
           "A 3D render of a wizard with a long grey beard in a dark blue gown with yellow stars and moons",
           "A 3D render of a golden dog sat down, staring ominously into your soul with black, lifeless eyes",
           "A 3D render of Albert Einstein",
           "A 3D render of a purple monkey, its fingers interlocked"];

    private const ClippyCharacter CLIPPY_CHARACTER_INVALID = (ClippyCharacter)(-1);
    private const ClippyCharacter CLIPPY_CHARACTER_MAX = (ClippyCharacter.Bonzi + 1);

    private const ClippyFont CLIPPY_FONT_INVALID = (ClippyFont)(-1);
    private const ClippyFont CLIPPY_FONT_MAX = (ClippyFont.NokiaPure + 1);

    [SlashCommand("clippy", "Generates an image of clippy asking a question.", runMode: RunMode.Async)]
    public async Task Command(
        [Summary(description: "What do you want the office assistant to say?")][MaxLength(4096)] string? text = null,
        [Summary(description: "What character do you want?")] ClippyCharacter character = CLIPPY_CHARACTER_INVALID,
        [Summary(description: "What font do you want?")] ClippyFont font = CLIPPY_FONT_INVALID,
        [Summary(description: "Add an image")] IAttachment? image = null,
        [Summary(description: "Enable antialiasing?")] bool antialiasing = false)
    {
        if (string.IsNullOrWhiteSpace(text) && image == null)
        {
            await RespondAsync("You'll need either an image or text!", ephemeral: true);
            return;
        }

        await DeferAsync();

        PickCharacterAndFont(Context.User, ref character, ref font);

        try
        {
            var (stream, description) = await GenerateClippyAsync(text, character, font, [image], antialiasing);
            await FollowupWithFileAsync(new FileAttachment(stream, "clippit.png", description: description));
            await stream.DisposeAsync();
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Something went wrong generating your image. {ex.Message} Sorry!");
        }
    }

    [MessageCommand("Clippy")]
    public async Task Clippy(IMessage msg)
    {
        await DeferAsync();

        var text = msg.Content;
        if (string.IsNullOrWhiteSpace(text) && msg.Attachments.Count == 0)
        {
            await FollowupAsync("There's nothing I can do with that that message!");
            return;
        }

        var character = CLIPPY_CHARACTER_INVALID;
        var font = CLIPPY_FONT_INVALID;
        PickCharacterAndFont(msg.Author, ref character, ref font);

        try
        {
            var (stream, description) = await GenerateClippyAsync(text, character, font, [.. msg.Attachments], false);
            await FollowupWithFileAsync(new FileAttachment(stream, "clippit.png", description: description));
            await stream.DisposeAsync();
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Something went wrong generating your image. {ex.Message} Sorry!");
        }
    }

    private void PickCharacterAndFont(IUser user, ref ClippyCharacter character, ref ClippyFont font)
    {
        if (character == CLIPPY_CHARACTER_INVALID)
        {
            character = (ClippyCharacter)(((user.Id >> 22) + 3) % (int)CLIPPY_CHARACTER_MAX);

            var characterOverrides = configuration.GetSection("CharacterOverrides");
            if (characterOverrides != null)
            {
                var characterOverride = characterOverrides.GetValue(user.Id.ToString(), CLIPPY_CHARACTER_INVALID);
                if (characterOverride != CLIPPY_CHARACTER_INVALID)
                    character = characterOverride;
            }
        }

        if (font == CLIPPY_FONT_INVALID)
        {
            font = (ClippyFont)(((user.Id >> 22) - 2) % (int)CLIPPY_FONT_MAX);

            if (font is ClippyFont.Webdings or ClippyFont.Wingdings)
                font += 3;

            var fontOverrides = configuration.GetSection("FontOverrides");
            if (fontOverrides != null)
            {
                var fontOverride = fontOverrides.GetValue(user.Id.ToString(), CLIPPY_FONT_INVALID);
                if (fontOverride != CLIPPY_FONT_INVALID)
                    font = fontOverride;
            }
        }
    }

    private async Task<(MemoryStream stream, string description)> GenerateClippyAsync(
        string? text,
        ClippyCharacter character,
        ClippyFont font,
        IAttachment?[] attachments,
        bool antialiasing)
    {
        using var httpClient = httpClientFactory.CreateClient("ClippyService");

        var characterString = ((int)character).ToString(CultureInfo.InvariantCulture);
        var fontString = ((int)font).ToString(CultureInfo.InvariantCulture);
        var antialiasString = antialiasing.ToString(CultureInfo.InvariantCulture);

        var content = new MultipartFormDataContent
        {
            { new StringContent(fontString), "font" },
            { new StringContent(antialiasString), "antialias" }
        };

        if (!string.IsNullOrWhiteSpace(text))
        {
            content.Add(new StringContent(text), "text");
        }

        var attachment = attachments.FirstOrDefault(a => a != null && a.Width != null && a.Height != null && a.ContentType.StartsWith("image/"));
        if (attachment != null)
        {
            using var discordHttpClient = httpClientFactory.CreateClient("Discord");
            using var attachmentRequest = new HttpRequestMessage(HttpMethod.Get, attachment.ProxyUrl);
            var attachmentResponse = await httpClient.SendAsync(attachmentRequest, HttpCompletionOption.ResponseContentRead);
            var attachmentStream = await attachmentResponse.Content.ReadAsStreamAsync();
            var streamContent = new StreamContent(attachmentStream);

            content.Add(streamContent, "attachment", "image.png");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{Uri.EscapeDataString(characterString)}/generate") { Content = content };
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }

        var memoryStream = new MemoryStream();
        using var stream = await response.Content.ReadAsStreamAsync();
        await stream.CopyToAsync(memoryStream);

        memoryStream.Seek(0, SeekOrigin.Begin);

        // TODO: this is embedded in the image file, use it
        var description = $"Microsoft Office Assistant, {CLIPPY_DISPLAY_NAMES[(int)character]} ({CLIPPY_DISPLAY_DESCRIPTIONS[(int)character]})";
        if (!string.IsNullOrWhiteSpace(text))
        {
            string alt = text;
            if (alt.Length > 512)
                alt = string.Concat(alt.AsSpan(0, 509), "...");
            description += $" saying '{alt}'";
        }
        if (attachment != null)
            description += $" with image";

        description += ".";

        return (memoryStream, description);
    }
}
