using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Informer.App.Utilities;

/// <summary>
/// Draws a small red "unread count" badge over the tray icon (top-right corner), the same
/// visual pattern used by Slack/Gmail/etc. Rendered fresh whenever the unread count
/// changes and assigned to TrayIcon.Icon.
///
/// This composites onto the app's own bundled icon (Assets/tray-icon.ico). If that file
/// can't be decoded for any reason, a plain accent-colored circle is drawn instead so the
/// badge itself still shows up rather than the whole thing silently failing.
/// </summary>
internal static class TrayBadgeRenderer
{
    private const int Size = 32;
    private static Bitmap? _baseIconCache;
    private static bool _baseIconLoadAttempted;

    public static WindowIcon Render(int unreadCount)
    {
        var target = new RenderTargetBitmap(new PixelSize(Size, Size));

        using (var ctx = target.CreateDrawingContext())
        {
            var baseIcon = GetBaseIcon();
            if (baseIcon is not null)
            {
                ctx.DrawImage(
                    baseIcon,
                    new Rect(0, 0, baseIcon.PixelSize.Width, baseIcon.PixelSize.Height),
                    new Rect(0, 0, Size, Size));
            }
            else
            {
                ctx.DrawEllipse(Brushes.DodgerBlue, null, new Point(Size / 2.0, Size / 2.0), Size / 2.0, Size / 2.0);
            }

            if (unreadCount > 0)
            {
                const double badgeRadius = 9.0;
                var center = new Point(Size - badgeRadius, badgeRadius);
                ctx.DrawEllipse(Brushes.Crimson, new Pen(Brushes.White, 1.5), center, badgeRadius, badgeRadius);

                var text = unreadCount > 9 ? "9+" : unreadCount.ToString();
                var formatted = new FormattedText(
                    text,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial", FontStyle.Normal, FontWeight.Bold),
                    10,
                    Brushes.White);

                var textOrigin = new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2);
                ctx.DrawText(formatted, textOrigin);
            }
        }

        return new WindowIcon(target);
    }

    private static Bitmap? GetBaseIcon()
    {
        if (_baseIconLoadAttempted)
        {
            return _baseIconCache;
        }

        _baseIconLoadAttempted = true;
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Informer/Assets/tray-icon.ico"));
            _baseIconCache = new Bitmap(stream);
        }
        catch
        {
            _baseIconCache = null; // fall back to the plain circle in Render()
        }

        return _baseIconCache;
    }
}