using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Keto_Tracker2.Views;

namespace Keto_Tracker2
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private GestureDetector? _swipeDetector;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            float density = Resources?.DisplayMetrics?.Density ?? 1f;
            _swipeDetector = new GestureDetector(this, new SwipeListener(density));
        }

        // Look at every touch before child views get it, so a horizontal swipe is detected
        // anywhere on the screen (calendar, chart, scroll views, text fields...).
        // The event is always passed on unchanged, so taps and scrolling keep working.
        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            if (e is not null)
                _swipeDetector?.OnTouchEvent(e);
            return base.DispatchTouchEvent(e);
        }

        private sealed class SwipeListener : GestureDetector.SimpleOnGestureListener
        {
            private readonly float _minDistancePx;
            private readonly float _minVelocityPx;

            public SwipeListener(float density)
            {
                _minDistancePx = 80f * density;   // finger must travel at least ~80dp sideways
                _minVelocityPx = 300f * density;  // and move reasonably fast
            }

            public override bool OnFling(MotionEvent? e1, MotionEvent e2, float velocityX, float velocityY)
            {
                if (e1 is null) return false;

                float dx = e2.GetX() - e1.GetX();
                float dy = e2.GetY() - e1.GetY();

                // Mostly horizontal, long enough and fast enough.
                if (Math.Abs(dx) < _minDistancePx) return false;
                if (Math.Abs(dx) < 2f * Math.Abs(dy)) return false;
                if (Math.Abs(velocityX) < _minVelocityPx) return false;

                var target = SwipeNavigation.CurrentTarget;
                if (target is null) return false;

                if (dx < 0) target.OnSwipeLeft();
                else target.OnSwipeRight();
                return true;
            }
        }
    }
}
