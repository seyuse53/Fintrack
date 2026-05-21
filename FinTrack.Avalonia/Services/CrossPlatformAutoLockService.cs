using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using FinTrack.Core.Services;

namespace FinTrack.Avalonia.Services;

public static class CrossPlatformAutoLockService
{
    private static DispatcherTimer? _timer;
    private static DateTime _lastActivity;
    private static bool _isEnabled;
    private static int _timeoutMinutes;
    private static bool _isLocked;

    public static event Action? OnLockTriggered;

    public static void Start()
    {
        Reconfigure();
        _lastActivity = DateTime.Now;

        // Listen to global pointer and key events using Tunnel strategy to intercept them early
        InputElement.PointerMovedEvent.AddClassHandler<TopLevel>((sender, e) => ResetActivity(), global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>((sender, e) => ResetActivity(), global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        InputElement.PointerWheelChangedEvent.AddClassHandler<TopLevel>((sender, e) => ResetActivity(), global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        InputElement.KeyDownEvent.AddClassHandler<TopLevel>((sender, e) => ResetActivity(), global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private static void ResetActivity()
    {
        _lastActivity = DateTime.Now;
    }

    public static void Reconfigure()
    {
        var (enabled, timeoutMin) = SettingsManager.GetAutoLockSettings();
        _isEnabled = enabled;
        _timeoutMinutes = timeoutMin;
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

        string? currentActiveKey = SettingsManager.ActiveDataKey;
        
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
}
