using System;
using System.Windows.Interop;
using System.Windows.Threading;

namespace FinTrack.WPF.Services
{
    public static class AutoLockService
    {
        private static DispatcherTimer? _timer;
        private static DateTime _lastActivity;
        private static bool _isEnabled;
        private static int _timeoutMinutes;
        private static bool _isLocked;

        public static event Action? OnLockTriggered;

        public static void Start()
        {
            // Initial configuration reading
            Reconfigure();

            _lastActivity = DateTime.Now;

            // Setup the global Windows message pump hook to listen to mouse/keyboard activity
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;

            // Timer to check lock state every second
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        public static void Reconfigure()
        {
            var (enabled, timeoutMin) = FinTrack.Core.Services.SettingsManager.GetAutoLockSettings();
            _isEnabled = enabled;
            _timeoutMinutes = timeoutMin;
            
            // Re-evaluating immediately resets the activity timer so it doesn't instantly lock 
            // if we just switched the minutes config to a lower value.
            _lastActivity = DateTime.Now;
        }

        public static void MarkUnlocked()
        {
            _isLocked = false;
            _lastActivity = DateTime.Now;
        }

        private static void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isEnabled || _isLocked)
                return;

            string? currentActiveKey = FinTrack.Core.Services.SettingsManager.ActiveDataKey;
            
            // Only lock if the user is actually technically logged in
            if (string.IsNullOrEmpty(currentActiveKey))
                return;

            var timeSinceLastActivity = DateTime.Now - _lastActivity;
            if (timeSinceLastActivity.TotalMinutes >= _timeoutMinutes)
            {
                TriggerLock();
            }
        }

        private static void TriggerLock()
        {
            if (_isLocked) return;
            _isLocked = true;
            OnLockTriggered?.Invoke();
        }

        private static void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            // We intercept all messages going to the app. 
            // We care about mouse movement, clicking, and keyboard typing.
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_MBUTTONDOWN = 0x0207;
            const int WM_MOUSEWHEEL = 0x020A;
            const int WM_KEYDOWN = 0x0100;
            const int WM_SYSKEYDOWN = 0x0104;

            if (msg.message == WM_MOUSEMOVE || 
                msg.message == WM_LBUTTONDOWN || msg.message == WM_RBUTTONDOWN || msg.message == WM_MBUTTONDOWN ||
                msg.message == WM_MOUSEWHEEL || 
                msg.message == WM_KEYDOWN || msg.message == WM_SYSKEYDOWN)
            {
                // User is active!
                _lastActivity = DateTime.Now;
            }
        }
    }
}
