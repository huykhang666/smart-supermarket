using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop;

public static class ImageHelper
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly ConcurrentDictionary<string, Image> _memoryCache = new();
    private static readonly string CacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");

    static ImageHelper()
    {
        try
        {
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }
        catch { }
    }

    /// <summary>
    /// Loads an image asynchronously into the PictureBox.
    /// Sets an immediate high-quality vector placeholder, then replaces it if the image loads successfully.
    /// </summary>
    public static void LoadProductImageAsync(PictureBox picBox, string? imageUrl, string? categoryName, string? productName = null)
    {
        if (picBox == null || picBox.IsDisposed) return;

        // 1. Immediately set modern category-themed placeholder
        var placeholder = GetPlaceholderImage(categoryName, productName, picBox.Width, picBox.Height);
        picBox.Image = placeholder;

        if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.Equals("/images/products/no-image.png", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 2. Check local file
        if (File.Exists(imageUrl))
        {
            try
            {
                using var stream = new FileStream(imageUrl, FileMode.Open, FileAccess.Read, FileShare.Read);
                var localImg = Image.FromStream(stream);
                picBox.Image = localImg;
                return;
            }
            catch { }
        }

        // 3. Check web URL (http:// or https://)
        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Check memory cache first
            if (_memoryCache.TryGetValue(imageUrl, out var cachedImg))
            {
                picBox.Image = cachedImg;
                return;
            }

            // Check disk cache
            string cacheKey = GetMd5Hash(imageUrl);
            string cachedFilePath = Path.Combine(CacheDirectory, $"{cacheKey}.dat");

            if (File.Exists(cachedFilePath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(cachedFilePath);
                    using var ms = new MemoryStream(bytes);
                    var diskImg = Image.FromStream(ms);
                    _memoryCache[imageUrl] = diskImg;
                    picBox.Image = diskImg;
                    return;
                }
                catch { }
            }

            // Download asynchronously in background without freezing UI
            Task.Run(async () =>
            {
                try
                {
                    byte[] data = await _httpClient.GetByteArrayAsync(imageUrl);
                    if (data != null && data.Length > 0)
                    {
                        // Save to disk cache
                        try
                        {
                            await File.WriteAllBytesAsync(cachedFilePath, data);
                        }
                        catch { }

                        using var ms = new MemoryStream(data);
                        var downloadedImg = Image.FromStream(ms);
                        _memoryCache[imageUrl] = downloadedImg;

                        if (!picBox.IsDisposed && picBox.IsHandleCreated)
                        {
                            picBox.BeginInvoke(new Action(() =>
                            {
                                if (!picBox.IsDisposed)
                                {
                                    picBox.Image = downloadedImg;
                                }
                            }));
                        }
                    }
                }
                catch
                {
                    // Network failed or offline - keep the clean vector placeholder
                }
            });
        }
    }

    public static Image GetPlaceholderImage(string? categoryName, string? productName, int width, int height)
    {
        if (width <= 0) width = 205;
        if (height <= 0) height = 140;

        string cacheKey = $"ph_{categoryName}_{width}_{height}";
        if (_memoryCache.TryGetValue(cacheKey, out var existing))
        {
            return existing;
        }

        var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Category color theme palette
            var (bgColor, accentColor, iconText, categoryShort) = GetCategoryTheme(categoryName);

            // Clean background fill
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, 0, 0, width, height);
            }

            // Subtle inner frame
            using (var pen = new Pen(Color.FromArgb(30, accentColor), 1f))
            {
                g.DrawRectangle(pen, 1, 1, width - 3, height - 3);
            }

            // Central icon badge (circle or rounded pill)
            int badgeSize = 54;
            int badgeX = (width - badgeSize) / 2;
            int badgeY = 22;

            using (var badgeBrush = new SolidBrush(Color.FromArgb(240, Color.White)))
            {
                g.FillEllipse(badgeBrush, badgeX, badgeY, badgeSize, badgeSize);
            }
            using (var badgeBorder = new Pen(accentColor, 1.5f))
            {
                g.DrawEllipse(badgeBorder, badgeX, badgeY, badgeSize, badgeSize);
            }

            // Draw Category Icon (Using Segoe UI Emoji with Segoe UI fallback)
            try
            {
                using var emojiFont = new Font("Segoe UI Emoji", 20f, FontStyle.Regular);
                using var emojiBrush = new SolidBrush(accentColor);
                var sfCenter = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(iconText, emojiFont, emojiBrush, new RectangleF(badgeX, badgeY, badgeSize, badgeSize), sfCenter);
            }
            catch
            {
                // Fallback text icon
                using var fbFont = new Font("Segoe UI", 14f, FontStyle.Bold);
                using var fbBrush = new SolidBrush(accentColor);
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("SP", fbFont, fbBrush, new RectangleF(badgeX, badgeY, badgeSize, badgeSize), sfCenter);
            }

            // Category label below badge
            using (var fontCat = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var brushCat = new SolidBrush(accentColor))
            {
                var sfCat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(categoryShort, fontCat, brushCat, new RectangleF(0, badgeY + badgeSize + 8, width, 22), sfCat);
            }

            // "Hình ảnh sản phẩm" subtle note
            using (var fontNote = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (var brushNote = new SolidBrush(Color.FromArgb(140, accentColor)))
            {
                var sfNote = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Smart SuperMarket", fontNote, brushNote, new RectangleF(0, height - 20, width, 16), sfNote);
            }
        }

        _memoryCache[cacheKey] = bmp;
        return bmp;
    }

    private static (Color bg, Color accent, string icon, string name) GetCategoryTheme(string? categoryName)
    {
        string cat = (categoryName ?? "").ToLowerInvariant();

        if (cat.Contains("nước") || cat.Contains("uống") || cat.Contains("giải khát"))
            return (Color.FromArgb(235, 245, 255), Color.FromArgb(14, 116, 144), "🥤", "Nước giải khát");

        if (cat.Contains("sữa"))
            return (Color.FromArgb(240, 244, 255), Color.FromArgb(79, 70, 229), "🥛", "Sữa & Chế phẩm");

        if (cat.Contains("bánh") || cat.Contains("kẹo") || cat.Contains("vặt") || cat.Contains("snack"))
            return (Color.FromArgb(254, 247, 235), Color.FromArgb(217, 119, 6), "🍪", "Bánh kẹo & Snack");

        if (cat.Contains("rau") || cat.Contains("củ") || cat.Contains("quả") || cat.Contains("trái cây"))
            return (Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105), "🥬", "Rau củ quả tươi");

        if (cat.Contains("thịt") || cat.Contains("hải sản") || cat.Contains("trứng") || cat.Contains("cá"))
            return (Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38), "🥩", "Thực phẩm tươi");

        if (cat.Contains("gia vị") || cat.Contains("khô"))
            return (Color.FromArgb(255, 247, 237), Color.FromArgb(234, 88, 12), "🧂", "Gia vị & Đồ khô");

        if (cat.Contains("ăn liền") || cat.Contains("mì") || cat.Contains("hộp"))
            return (Color.FromArgb(254, 252, 232), Color.FromArgb(202, 138, 4), "🍜", "Đồ ăn liền");

        if (cat.Contains("hóa phẩm") || cat.Contains("giặt") || cat.Contains("tẩy"))
            return (Color.FromArgb(236, 254, 255), Color.FromArgb(8, 145, 178), "🧼", "Hóa phẩm & Giặt xả");

        if (cat.Contains("chăm sóc") || cat.Contains("cá nhân") || cat.Contains("tắm") || cat.Contains("gội"))
            return (Color.FromArgb(255, 241, 242), Color.FromArgb(225, 29, 72), "🧴", "Chăm sóc cá nhân");

        if (cat.Contains("đồ dùng") || cat.Contains("nhà bếp") || cat.Contains("gia đình"))
            return (Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105), "🍳", "Đồ dùng nhà bếp");

        return (Color.FromArgb(248, 250, 252), Color.FromArgb(15, 108, 189), "📦", "Sản phẩm");
    }

    private static string GetMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
