using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Informer.App.Utilities;

internal static class ScreenPositioning
{
    public static PixelPoint BottomRight(Screen screen, double widthDip, double heightDip, double marginDip)
    {
        var scaling = screen.Scaling;
        var workingArea = screen.WorkingArea;

        var widthPx = widthDip * scaling;
        var heightPx = heightDip * scaling;
        var marginPx = marginDip * scaling;

        var x = workingArea.Right - widthPx - marginPx;
        var y = workingArea.Bottom - heightPx - marginPx;

        return new PixelPoint((int)x, (int)y);
    }

    public static Screen? PickScreen(Screens screens) =>
        screens.Primary ?? (screens.All.Count > 0 ? screens.All[0] : null);
}