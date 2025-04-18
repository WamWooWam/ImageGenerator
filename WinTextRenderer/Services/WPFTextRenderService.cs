﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinTextRenderer.Services;

public class WPFTextRenderService(
    ILogger<WPFTextRenderService> logger,
    IDispatcher dispatcher) : ITextRenderService
{
    public async Task RenderTextAsync(string text, TextRenderOptions options, Stream target)
    {
        await dispatcher.InvokeAsync(() =>
        {
            var converter = new FontFamilyConverter();
            var background = (Color)ColorConverter.ConvertFromString(options.Background)!;

            var textBlock = new TextBlock(new Run(text))
            {
                FontFamily = (FontFamily)converter.ConvertFromString(options.FontFamily)!,
                FontSize = (options.FontSize / 72.0) * 96.0,
                TextWrapping = options.Wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(options.Foreground)!),
                Background = new SolidColorBrush(background),
            };

            textBlock.Typography.Kerning = options.Kerning;

            if (!options.Antialiasing)
            {
                TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.Aliased);
                TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Display);
            }
            else
            {
                TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.ClearType);
                if (!options.GDICompatible)
                {
                    TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Ideal);
                }
                else
                {
                    TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Display);
                }

                if (background.A == 255)
                {
                    RenderOptions.SetClearTypeHint(textBlock, ClearTypeHint.Enabled);
                }
            }


            textBlock.Measure(new Size(options.MaxWidth, options.MaxHeight));
            textBlock.Arrange(new Rect(new Point(0, 0), textBlock.DesiredSize));

            logger.LogDebug("Rendered {Text}, DesiredSize={DesiredSize}", text, textBlock.DesiredSize);

            var renderTarget = new RenderTargetBitmap(
                (int)Math.Ceiling(textBlock.DesiredSize.Width * (options.Dpi / 96.0)),
                (int)Math.Ceiling(textBlock.DesiredSize.Height * (options.Dpi / 96.0)),
                (float)options.Dpi,
                (float)options.Dpi,
                PixelFormats.Default);

            renderTarget.Render(textBlock);

            var encoder = new PngBitmapEncoder() { Interlace = PngInterlaceOption.Off };
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));
            //encoder.Metadata.Title = text;
            encoder.Save(target);

            target.Seek(0, SeekOrigin.Begin);
        });
    }
}
