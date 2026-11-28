using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Speech;
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

        // ── Voice input ──────────────────────────────────────────────────────
        private const int SpeechRequestCode = 4711;
        private static TaskCompletionSource<IReadOnlyList<string>?>? _speechTcs;

        /// <summary>
        /// Shows the system speech dialog (Google / Samsung recogniser) and returns the recognised
        /// alternatives. The recogniser app records the audio itself, so this app needs no
        /// microphone permission. Offline recognition is requested.
        /// </summary>
        public static Task<IReadOnlyList<string>?> ListenForSpeechAsync(string language)
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                ?? throw new InvalidOperationException("Приложение не готово к голосовому вводу.");

            var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            intent.PutExtra(RecognizerIntent.ExtraLanguage, language);
            intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, language);
            intent.PutExtra(RecognizerIntent.ExtraPreferOffline, true);
            intent.PutExtra(RecognizerIntent.ExtraMaxResults, 5);
            intent.PutExtra(RecognizerIntent.ExtraPrompt,
                language.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
                    ? "Скажите, например: «вешу 90» или «в 15:00 ел печёнку»"
                    : "Say e.g. \"weight 90\" or \"at 3 pm ate eggs\"");

            _speechTcs?.TrySetResult(null);
            var tcs = new TaskCompletionSource<IReadOnlyList<string>?>();
            _speechTcs = tcs;
            try
            {
                activity.StartActivityForResult(intent, SpeechRequestCode);
            }
            catch (ActivityNotFoundException)
            {
                _speechTcs = null;
                throw new InvalidOperationException(
                    "На телефоне не найдено распознавание речи. Установите или включите приложение Google (голосовой ввод).");
            }
            return tcs.Task;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode != SpeechRequestCode) return;

            var tcs = _speechTcs;
            _speechTcs = null;
            if (tcs is null) return;

            if (resultCode != Result.Ok || data is null)
            {
                tcs.TrySetResult(null);
                return;
            }

            var results = data.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
            var list = results?.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            tcs.TrySetResult(list is { Count: > 0 } ? list : null);
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
