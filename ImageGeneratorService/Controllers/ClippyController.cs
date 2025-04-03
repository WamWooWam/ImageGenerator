using System.Globalization;
using System.Text;
using ImageGeneratorService.Resources.Clippy;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageGeneratorService.Controllers;

[ApiController]
public class ClippyController(IHttpClientFactory httpClientFactory) : Controller
{
    private static readonly Rgba32 CLIPPY_BACKGROUND = new(0xFF, 0xFF, 0xCC); // clippy background colour

    private static readonly Lazy<Image<Rgba32>> CLIPPY_TOP_LEFT = new(() =>
    {
        var image = Image.Load<Rgba32>(ClippyResources.ClippyCorner);
        return image;
    });

    private static readonly Lazy<Image<Rgba32>> CLIPPY_TOP_RIGHT = new(() =>
    {
        var image = Image.Load<Rgba32>(ClippyResources.ClippyCorner);
        image.Mutate(i => i.Rotate(RotateMode.Rotate90));
        return image;
    });

    private static readonly Lazy<Image<Rgba32>> CLIPPY_BOTTOM_RIGHT = new(() =>
    {
        var image = Image.Load<Rgba32>(ClippyResources.ClippyCorner);
        image.Mutate(i => i.Rotate(RotateMode.Rotate180));
        return image;
    });

    private static readonly Lazy<Image<Rgba32>> CLIPPY_BOTTOM_LEFT = new(() =>
    {
        var image = Image.Load<Rgba32>(ClippyResources.ClippyCorner);
        image.Mutate(i => i.Rotate(RotateMode.Rotate270));
        return image;
    });

    private static readonly Lazy<Image<Rgba32>> CLIPPY_ARROW
        = new(() => Image.Load<Rgba32>(ClippyResources.ClippyArrow));

    private static readonly string[] CLIPPY_CHARACTERS
        = ["clippy", "dot", "hoverbot", "nature", "office", "powerpup", "scribble", "wizard", "rover", "einstein", "bonzi"];

    private const int CLIPPY_TOP_HEIGHT = 8;
    private const int CLIPPY_CORNER_SIZE = 8;
    private const int CLIPPY_BOTTOM_HEIGHT = 23;
    private const int CLIPPY_DEFAULT_MAX_WIDTH = 300;
    private const int CLIPPY_MIN_WIDTH = 150;
    private const int CLIPPY_MIN_WIDTH_WITH_IMAGE = 200;

    internal enum ClippyCharacter
    {
        Clippy,
        Dot,
        HoverBot,
        Nature,
        Office,
        PowerPup,
        Scribble,
        Wizard,
        Rover,
        Einstein,
        Bonzi
    }

    internal static ClippyCharacter ToClippyCharacter(string str)
    {
        if (int.TryParse(str, CultureInfo.InvariantCulture, out var num) && num is >= 0 and < (int)CLIPPY_CHARACTER_MAX)
            return (ClippyCharacter)num;

        if (Enum.TryParse<ClippyCharacter>(str, true, out var character))
            return character;

        return str.ToLowerInvariant() switch
        {
            "clippit" => ClippyCharacter.Clippy,
            "clippy" => ClippyCharacter.Clippy,
            "dot" => ClippyCharacter.Dot,
            "the_dot" => ClippyCharacter.Dot,
            "f1" => ClippyCharacter.HoverBot,
            "mother_earth" => ClippyCharacter.Nature,
            "nature" => ClippyCharacter.Nature,
            "office" => ClippyCharacter.Office,
            "office_logo" => ClippyCharacter.Office,
            "rocky" => ClippyCharacter.PowerPup,
            "power_pup" => ClippyCharacter.PowerPup,
            "links" => ClippyCharacter.Scribble,
            "scribble" => ClippyCharacter.Scribble,
            "cat" => ClippyCharacter.Scribble,
            "merlin" => ClippyCharacter.Wizard,
            "wizard" => ClippyCharacter.Wizard,
            "rover" => ClippyCharacter.Rover,
            "dog" => ClippyCharacter.Rover,
            "the_genius" => ClippyCharacter.Einstein,
            "einstein" => ClippyCharacter.Einstein,
            "bonzi" => ClippyCharacter.Bonzi,
            "that_fucking_purple_monkey" => ClippyCharacter.Bonzi,
            "" => ClippyCharacter.Clippy,
            _ => throw new ArgumentException("Not a valid clippy character", nameof(str))
        };
    }

    internal enum ClippyFont
    {
        Tahoma,
        ComicSans,
        MSSansSerif,
        Times,
        CourierNew,
        MSGothic
    }

    internal static ClippyFont ToClippyFont(string str)
    {
        if (int.TryParse(str, CultureInfo.InvariantCulture, out var num) && num is >= 0 and < (int)CLIPPY_FONT_MAX)
            return (ClippyFont)num;

        if (Enum.TryParse<ClippyFont>(str, true, out var font))
            return font;

        return str.ToLowerInvariant() switch
        {
            "serif" => ClippyFont.Times,
            "tahoma" => ClippyFont.Tahoma,
            "comic" => ClippyFont.ComicSans,
            "comic_sans" => ClippyFont.ComicSans,
            "sans" => ClippyFont.ComicSans,
            "sans_serif" => ClippyFont.MSSansSerif,
            "microsoft_sans_serif" => ClippyFont.MSSansSerif,
            "ms_sans_serif" => ClippyFont.MSSansSerif,
            "times" => ClippyFont.Times,
            "times_new_roman" => ClippyFont.Times,
            "courier" => ClippyFont.CourierNew,
            "courier_new" => ClippyFont.CourierNew,
            "monospace" => ClippyFont.CourierNew,
            "gothic" => ClippyFont.MSGothic,
            "ms_gothic" => ClippyFont.MSGothic,
            "" => ClippyFont.ComicSans,
            _ => throw new ArgumentException("Not a valid clippy font", nameof(str))
        };
    }

    private const ClippyCharacter CLIPPY_CHARACTER_INVALID = (ClippyCharacter)(-1);
    private const ClippyCharacter CLIPPY_CHARACTER_MAX = (ClippyCharacter.Bonzi + 1);

    private const ClippyFont CLIPPY_FONT_INVALID = (ClippyFont)(-1);
    private const ClippyFont CLIPPY_FONT_MAX = (ClippyFont.MSGothic + 1);
    public record class ClippyOptions(
        [FromForm(Name = "text")] string? Text = null,
        [FromForm(Name = "font")] string Font = "",
        [FromForm(Name = "antialias")] bool Antialias = false,
        [FromForm(Name = "attachment")] IFormFile? Attachment = null);

    [HttpGet("{character=clippy}/generate")]
    public Task GenerateAsync(string text, string character = "", string font = "", bool antialias = false)
    {
        return GenerateAsync(character, new ClippyOptions(text, font, antialias));
    }
    [HttpPost("{character=clippy}/generate")]
    public async Task GenerateAsync([FromRoute] string character, [FromForm] ClippyOptions formData)
    {
        Image<Rgba32>? attachmentImage = null;
        string? text = null;
        try
        {
            if (formData.Attachment != null)
            {
                if (formData.Attachment.Length is 0 or > 2 * 1024 * 1024)
                {
                    await WriteErrorMessageAsync(400, "'attachment' must have a specified size, and may not be larger than 2MB.");
                    return;
                }

                var options = new DecoderOptions() { TargetSize = new Size(CLIPPY_DEFAULT_MAX_WIDTH, 800) };
                using var attachmentStream = formData.Attachment.OpenReadStream();
                attachmentImage = await Image.LoadAsync<Rgba32>(options, attachmentStream);
            }

            if (!string.IsNullOrWhiteSpace(formData.Text))
            {
                text = UnescapeText(formData.Text);
            }

            if (string.IsNullOrWhiteSpace(text) && attachmentImage == null)
            {
                await WriteErrorMessageAsync(400, "Either 'text' or 'attachment' must be specified.");
                return;
            }

            var clippyCharacter = ToClippyCharacter(character);
            var clippyFont = ToClippyFont(formData.Font);

            var stream = this.Response.Body;
            this.Response.ContentType = "image/png";
            await GenerateClippyAsync(stream, text, clippyCharacter, clippyFont, formData.Antialias, attachmentImage);
        }
        catch (UnknownImageFormatException)
        {
            await WriteErrorMessageAsync(400, "Image format was unrecognised.");
            return;
        }
        catch (Exception ex)
        {
            await WriteErrorMessageAsync(500, "Unable to generate the requested image. " + ex.Message);
            return;
        }
        finally
        {
            attachmentImage?.Dispose();
        }
    }

    private async Task WriteErrorMessageAsync(int code, string text)
    {
        this.Response.StatusCode = code;
        this.Response.ContentType = "text/plain";
        await this.Response.WriteAsync(text);
    }

    private async Task GenerateClippyAsync(
        Stream target,
        string? text,
        ClippyCharacter character,
        ClippyFont font,
        bool antialias,
        Image<Rgba32>? attachment = null)
    {
        Image<Rgba32>? textImage = null;
        try
        {
            using var characterImage = Image.Load<Rgba32>((byte[])ClippyResources.ResourceManager.GetObject(CLIPPY_CHARACTERS[(int)character])!);

            var topLeft = CLIPPY_TOP_LEFT.Value;
            var topRight = CLIPPY_TOP_RIGHT.Value;
            var bottomLeft = CLIPPY_BOTTOM_LEFT.Value;
            var bottomRight = CLIPPY_BOTTOM_RIGHT.Value;
            var arrow = CLIPPY_ARROW.Value;

            var basicPen = new SolidPen(Brushes.Solid(Color.Black), 1);
            var imageWidth = CLIPPY_DEFAULT_MAX_WIDTH;
            Size size;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var clippyFont = font switch
                {
                    ClippyFont.Tahoma => ("Tahoma", 10f),
                    ClippyFont.Times => ("Times New Roman", 11f),
                    ClippyFont.ComicSans => ("Comic Sans MS", 11f),
                    ClippyFont.MSSansSerif => ("Microsoft Sans Serif", 10.5f),
                    ClippyFont.MSGothic => ("MS Gothic", 10.5f),
                    ClippyFont.CourierNew => ("Courier New", 11f),
                    _ => throw new InvalidOperationException()
                };

                using var httpClient = httpClientFactory.CreateClient("TextRenderService");
                using var resp = await httpClient.GetStreamAsync("render" +
                    $"?text={Uri.EscapeDataString(text)}" +
                    $"&font={Uri.EscapeDataString($"{clippyFont.Item1}, Noto Emoji, MS Gothic, Times New Roman, Seoge UI Symbol")}" +
                    $"&size={clippyFont.Item2}" +
                    $"&maxWidth={imageWidth - 20}" +
                    $"&antialias={antialias}" +
                    $"&gdi=true");

                textImage = await Image.LoadAsync<Rgba32>(resp);
                size = textImage.Size;
            }
            else
            {
                size = new Size();
            }

            imageWidth = Math.Max((int)size.Width + 20, attachment != null ? CLIPPY_MIN_WIDTH_WITH_IMAGE : CLIPPY_MIN_WIDTH);

            if (attachment != null)
            {
                var ratio = Math.Min((double)Math.Max(imageWidth, 200) / attachment.Width, 400.0 / attachment.Height);
                var width = (int)Math.Ceiling(attachment.Width * ratio);
                var height = (int)Math.Ceiling(attachment.Height * ratio);

                var resizeOptions = new ResizeOptions()
                {
                    Sampler = KnownResamplers.NearestNeighbor,
                    Size = new Size(width - 2, height),
                    Mode = ResizeMode.Max
                };

                attachment.Mutate(m => m.Resize(resizeOptions)
                    .Quantize(KnownQuantizers.WebSafe));
            }

            using var topImage = new Image<Rgba32>(imageWidth, 8);
            topImage.Mutate(m => m
                        .SetGraphicsOptions(options => options.Antialias = false)
                        .Fill(CLIPPY_BACKGROUND, new RectangleF(CLIPPY_CORNER_SIZE, 0, imageWidth - (CLIPPY_CORNER_SIZE * 2), CLIPPY_CORNER_SIZE))
                        .DrawImage(topLeft, new Point(0, 0), 1.0f)
                        .DrawImage(topRight, new Point(imageWidth - CLIPPY_CORNER_SIZE, 0), 1.0f)
                        .DrawLine(basicPen, [new((CLIPPY_CORNER_SIZE - 1), 0), new(imageWidth - (CLIPPY_CORNER_SIZE), 0)])); // i hate off by ones

            var arrowPosition = ((imageWidth / 2.0f) - (characterImage.Width / 2)) + 4; // vibes based maths
            using var bottomImage = new Image<Rgba32>(imageWidth, CLIPPY_BOTTOM_HEIGHT);
            bottomImage.Mutate(m => m
                        .SetGraphicsOptions(options => options.Antialias = false)
                        .Fill(CLIPPY_BACKGROUND, new RectangleF(8, 0, imageWidth - (CLIPPY_CORNER_SIZE * 2), CLIPPY_CORNER_SIZE))
                        .DrawImage(bottomLeft, new Point(0, 0), 1.0f)
                        .DrawImage(bottomRight, new Point(imageWidth - CLIPPY_CORNER_SIZE, 0), 1.0f)
                        .DrawLine(basicPen, [new((CLIPPY_CORNER_SIZE - 1), (CLIPPY_CORNER_SIZE - 1)), new(imageWidth - (CLIPPY_CORNER_SIZE + 1), (CLIPPY_CORNER_SIZE - 1))])
                        .DrawImage(arrow, new Point((int)arrowPosition, 0), 1));

            if (attachment != null)
                size = new Size(size.Width, size.Height + attachment.Height + (textImage != null ? 5 : 0));

            var textRectangle = new Rectangle(10, CLIPPY_TOP_HEIGHT - 1, imageWidth, (int)(size.Height) + 1);
            var innerRectangle = new Rectangle(0, CLIPPY_TOP_HEIGHT, imageWidth, (int)(size.Height) + 2);

            var imageHeight = CLIPPY_TOP_HEIGHT + size.Height + CLIPPY_BOTTOM_HEIGHT + characterImage.Height;
            using var returnImage = new Image<Rgba32>(imageWidth, (int)imageHeight);
            returnImage.Mutate(m =>
            {
                m.SetGraphicsOptions(options => options.Antialias = false)
                 .Fill(Color.Transparent)
                 .Fill(CLIPPY_BACKGROUND, innerRectangle)
                 .DrawImage(topImage, new Point(0, 0), 1)
                 .DrawImage(bottomImage, new Point(0, (int)(CLIPPY_TOP_HEIGHT + size.Height)), 1);

                if (attachment != null)
                    m.DrawImage(attachment, new Point(Math.Max(1, (int)((imageWidth - attachment.Width) / 2.0f)), CLIPPY_TOP_HEIGHT + size.Height - attachment.Height), 1.0f);

                m.DrawLine(Color.Black, 1, [new PointF(innerRectangle.Left, innerRectangle.Top - 1), new PointF(innerRectangle.Left, innerRectangle.Bottom - 1)])
                 .DrawLine(Color.Black, 1, [new PointF(innerRectangle.Right - 1, innerRectangle.Top - 1), new PointF(innerRectangle.Right - 1, innerRectangle.Bottom - 1)]);

                if (textImage != null)
                    m.DrawImage(textImage, new Point(textRectangle.Left, textRectangle.Top), 1);

                m.DrawImage(characterImage, new Point((imageWidth - characterImage.Width) / 2, (int)(CLIPPY_TOP_HEIGHT + size.Height + CLIPPY_BOTTOM_HEIGHT)), 1);
            });

            var exifProfile = new ExifProfile();
            exifProfile.SetValue(ExifTag.Software, "Wam's Clippy Generator");
            exifProfile.SetValue(ExifTag.ImageDescription, "Microsoft Office Assistant" + (text != null ? " saying " + text : ""));
            returnImage.Metadata.ExifProfile = exifProfile;

            await returnImage.SaveAsPngAsync(target);

        }
        finally
        {
            textImage?.Dispose();
        }
    }

    private static string UnescapeText(string text)
    {
        if (text.Length is <= 0 or > 4096)
            throw new ArgumentException("Text must be at least 1 character but less than 4097 characters", nameof(text));

        var builder = new StringBuilder();
        var lastChar = char.MinValue;
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\')
            {
                if (lastChar == '\\')
                {
                    builder.Append('\\');
                    lastChar = char.MinValue;
                    continue;
                }

                lastChar = c;
                continue;
            }

            if (lastChar == '\\')
            {
                if (c == 'n')
                    builder.Append('\n');
                else if (c == 'r')
                    builder.Append('\r');
                else if (c == 't')
                    builder.Append('\t');
                else if (c == ' ')
                    builder.Append(' ');
                else
                    builder.Append(c);

                lastChar = c;
                continue;
            }

            builder.Append(c);
            lastChar = c;
        }

        text = builder.ToString();
        return text;
    }
}
