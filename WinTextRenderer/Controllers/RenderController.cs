using System.IO;
using Microsoft.AspNetCore.Mvc;
using WinTextRenderer.Services;
namespace WinTextRenderer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RenderController(ITextRenderService textRenderService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string text,
                                         string font = "",
                                         double size = 11.0,
                                         double maxWidth = double.PositiveInfinity,
                                         double maxHeight = double.PositiveInfinity,
                                         string foreground = "#000000",
                                         string background = "Transparent",
                                         bool wrap = true,
                                         bool antialias = true,
                                         bool kern = true,
                                         bool gdi = false)
    {
        var stream = new MemoryStream();
        var options = new TextRenderOptions(
            string.IsNullOrWhiteSpace(font) ? "Microsoft Sans Serif" : font,
            size,
            96,
            maxWidth,
            maxHeight,
            foreground,
            background,
            wrap,
            antialias,
            kern,
            gdi);

        await textRenderService.RenderTextAsync(text, options, stream);

        return File(stream, "image/png");
    }
}
