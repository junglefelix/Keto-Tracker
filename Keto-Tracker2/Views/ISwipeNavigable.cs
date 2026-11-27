using Microsoft.Maui.Devices;

namespace Keto_Tracker2.Views;

/// <summary>
/// A page that reacts to a horizontal swipe anywhere on screen.
/// On Android the swipe is detected globally in MainActivity, so it works even on top of
/// scroll views, collection views and other controls that swallow MAUI gesture recognizers.
/// On other platforms <see cref="SwipeNavigation.Attach"/> adds regular SwipeGestureRecognizers.
/// </summary>
public interface ISwipeNavigable
{
    /// <summary>Finger moved right to left: go to the next item (month / day).</summary>
    void OnSwipeLeft();

    /// <summary>Finger moved left to right: go to the previous item (month / day).</summary>
    void OnSwipeRight();
}

public static class SwipeNavigation
{
    /// <summary>
    /// Adds swipe recognizers to <paramref name="view"/> on platforms where the global
    /// Android detector is not used.
    /// </summary>
    public static void Attach(View view, ISwipeNavigable target)
    {
        if (DeviceInfo.Platform == DevicePlatform.Android) return;

        var left = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
        left.Swiped += (_, _) => target.OnSwipeLeft();
        var right = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
        right.Swiped += (_, _) => target.OnSwipeRight();
        view.GestureRecognizers.Add(left);
        view.GestureRecognizers.Add(right);
    }

    /// <summary>The page currently on screen, if it supports swipe navigation.</summary>
    public static ISwipeNavigable? CurrentTarget =>
        Shell.Current?.CurrentPage as ISwipeNavigable;
}
