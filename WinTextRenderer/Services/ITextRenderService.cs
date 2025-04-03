using System.IO;

namespace WinTextRenderer.Services;

public record struct TextRenderOptions(
    string FontFamily,
    double FontSize,
    double Dpi = 96.0f,
    double MaxWidth = double.PositiveInfinity,
    double MaxHeight = double.PositiveInfinity,
    string Foreground = "#FF000000",
    string Background = "Transparent",
    bool Wrap = true,
    bool Antialiasing = true,
    bool Kerning = true,
    bool GDICompatible = true);

public interface ITextRenderService
{
    public async Task<Stream> RenderTextAsync(string text)
    {
        var stream = new MemoryStream();
        await RenderTextAsync(text, stream);
        return stream;
    }

    public async Task RenderTextAsync(string text, Stream target)
    {
        await RenderTextAsync(text, new TextRenderOptions("Calibri", 11), target);
    }

    Task RenderTextAsync(string text, TextRenderOptions options, Stream target);
}
