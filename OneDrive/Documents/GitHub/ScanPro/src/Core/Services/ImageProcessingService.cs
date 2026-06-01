using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ScanPro.src.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ScanPro.src.Core.Services;

public class ImageProcessingService
{
    public async Task<Image> ProcessAsync(Image source, ScanSettings settings, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var img = source.CloneAs<Rgba32>();

            if (settings.AutoDeskew)
                img.Mutate(x => x.AutoOrient());

            if (settings.AutoCrop)
                img = AutoCrop(img);

            ApplyBrightnessContrast(img, settings.Brightness, settings.Contrast);

            return (Image)img;
        }, ct);
    }

    public bool IsBlankPage(Image image, float threshold = 0.97f)
    {
        var gray = image.CloneAs<Rgba32>();
        gray.Mutate(x => x.Grayscale());

        int white = 0, total = 0;
        gray.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                foreach (ref var px in row)
                {
                    total++;
                    if (px.R > 240) white++;
                }
            }
        });
        gray.Dispose();
        return total > 0 && (float)white / total >= threshold;
    }

    private static Image<Rgba32> AutoCrop(Image<Rgba32> image)
    {
        // Simple crop: remove 5px border noise
        int margin = 5;
        int w = Math.Max(1, image.Width - margin * 2);
        int h = Math.Max(1, image.Height - margin * 2);
        image.Mutate(x => x.Crop(new Rectangle(margin, margin, w, h)));
        return image;
    }

    private static void ApplyBrightnessContrast(Image<Rgba32> img, int brightness, int contrast)
    {
        float b = (brightness - 50) / 100f;
        float c = contrast / 50f;
        img.Mutate(x => x.Brightness(1f + b).Contrast(c));
    }
}
