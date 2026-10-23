using DynamicIsland.App.Animation;
using DynamicIsland.App.Shell;
using DynamicIsland.Core.Activities;
using DynamicIsland.Core.Battery;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DynamicIsland.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const double CompactIdleWidth = 180;
    private const double CompactActiveWidth = 280;
    private const double CompactHeight = 32;
    private const double CompactRadius = 16;
    private const double ExpandedWidth = 500;
    private const double ExpandedHeight = 380;
    private const double ExpandedRadius = 36;

    private readonly SpringAnimator _spring = new SpringAnimator(initial: 0);
    private readonly SpringAnimator _widthSpring = new(initial: 0);
    private readonly BatteryService _battery = new();
    private readonly ActivityManager _activities = new();

    private BatteryState? _lastBattery;
    private TimeSpan _lastRenderTime;
    private bool _isAnimating;
    private bool _introActive;
    private int _batteryPercent;

    public MainWindow()
    {
        InitializeComponent();
        Water.Filled += WaterFilled;

        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = 0;

        _activities.CurrentChanged += (_, _) => Dispatcher.InvokeAsync(ShowCurrentActivity);
        _battery.Changed += (_, s) => Dispatcher.InvokeAsync(() => UpdateBattery(s, animate: true));
        Closed += (_, _) => { _battery.Dispose(); _activities.Dispose(); };

        ApplyProgress();
        RefreshDate();
        UpdateBattery(_battery.Current, animate: false);

#if DEBUG
        // Klik kanan: simulasi charger dicolok/dicabut (65%).
        bool fakeCharging = false;
        Notch.MouseRightButtonDown += (_, _) =>
        {
            fakeCharging = !fakeCharging;
            UpdateBattery(new BatteryState(65, fakeCharging, true), animate: true);
        };

        // Klik tengah (roda mouse): putar demo
        // 1 musik -> 2 timer -> 3 baterai 15% -> 4 baterai 8% -> 0 selesai.
        int demo = 0;
        Notch.MouseDown += (_, e) =>
        {
            if (e.ChangedButton != MouseButton.Middle) return;
            demo = (demo + 1) % 5;

            switch (demo)
            {
                case 1:
                    _activities.Show(new LiveActivity("demo", ActivityPriority.Low,
                        new ActivitySlot(ActivityIcon.Music),
                        new ActivitySlot(Text: "Playing")));
                    break;
                case 2:
                    _activities.Show(new LiveActivity("demo", ActivityPriority.Low,
                        new ActivitySlot(ActivityIcon.Timer, null, ActivityTone.Blue),
                        new ActivitySlot(Text: "04:32", Tone: ActivityTone.Blue)));
                    break;
                case 3: // Medium mengalahkan Low: baterai menimpa timer
                    UpdateBattery(new BatteryState(15, false, true), animate: true);
                    break;
                case 4: // High
                    UpdateBattery(new BatteryState(8, false, true), animate: true);
                    break;
                default:
                    _activities.Remove("demo");
                    UpdateBattery(new BatteryState(65, false, true), animate: true);
                    break;
            }
        };
#endif


    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowStyleHelper.MakeOverlay(new WindowInteropHelper(this).Handle);
    }

    private void NotchMouseEnter(object sender, MouseEventArgs e)
    {
        RefreshDate();
        AnimateTo(1);
    }

    private void NotchMouseLeave(object sender, MouseEventArgs e) => AnimateTo(0);
    private void ShowCurrentActivity()
    {
        var a = _activities.Current;

        if (a?.Left is { } left) LeftSlot.Show(left); else LeftSlot.Hide();
        if (a?.Right is { } right) RightSlot.Show(right); else RightSlot.Hide();

        _widthSpring.Target = a is null ? 0 : 1;
        EnsureLoop();
    }

    private void UpdateBattery(BatteryState state, bool animate)
    {
        BatteryPanel.Visibility = state.HasBattery ? Visibility.Visible : Visibility.Collapsed;
        BatteryText.Text = $"{state.Percent}%";
        _batteryPercent = state.Percent;

        bool chargeJustStarted = animate && state.IsCharging
                                 && _lastBattery is { IsCharging: false };

        Battery.IsCharging = state.IsCharging;

        if (chargeJustStarted)
        {
            StartChargeTransition();
        }
        else
        {
            if (!state.IsCharging) CancelChargeTransition();
            if (!_introActive) Battery.SetLevel(state.Percent, animate);
        }

        // Aktivitas compact yang bergantung pada baterai
        if (!state.IsCharging) _activities.Remove(BatteryActivities.ChargingId);

        var low = BatteryActivities.LowBattery(state);
        if (low is null) _activities.Remove(BatteryActivities.LowId);
        else _activities.Show(low);

        _lastBattery = state;
    }

    private void StartChargeTransition()
    {
        _introActive = true;
        ApplyProgress(); 
        Water.Start();
    }

    private void CancelChargeTransition()
    {
        if (!_introActive) return;
        _introActive = false;
        Water.Cancel();
        Water.Visibility = Visibility.Collapsed;
        ApplyProgress();
    }

    private void WaterFilled(object? sender, EventArgs e)
    {
        if (!_introActive) return;
        _introActive = false;

        // Layar sudah hijau penuh: render UI di bawahnya, lalu pudarkan hijaunya.
        ApplyProgress();
        Battery.PlayChargeIntro(_batteryPercent);
        _activities.Show(BatteryActivities.Charging(_batteryPercent)); // tampil 4 detik di compact

        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(450))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        fade.Completed += (_, _) =>
        {
            if (!_introActive) Water.Visibility = Visibility.Collapsed;
        };
        Water.BeginAnimation(OpacityProperty, fade);
    }

    private void RefreshDate() => DateText.Text = DateTime.Now.ToString("dd MMM yyyy · HH:mm");

    private void AnimateTo(double target)
    {
        _spring.Target = target;
        EnsureLoop();
    }

    private void EnsureLoop()
    {
        if (_isAnimating) return; // loop sudah jalan, cukup ganti target

        _isAnimating = true;
        _lastRenderTime = TimeSpan.Zero;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = ((RenderingEventArgs)e).RenderingTime;
        double dt = _lastRenderTime == TimeSpan.Zero
            ? 1.0 / 60
            : (now - _lastRenderTime).TotalSeconds;

        if (dt <= 0) return;
        _lastRenderTime = now;

        _spring.Step(dt);
        _widthSpring.Step(dt);
        ApplyProgress();

        // Berhenti hanya kalau KEDUA spring sudah diam.
        if (_spring.IsSettled && _widthSpring.IsSettled)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isAnimating = false;
        }
    }

    private void ApplyProgress()
    {
        double t = _spring.Value;
        double compactWidth = Lerp(CompactIdleWidth, CompactActiveWidth, _widthSpring.Value);

        Notch.Width = Math.Max(0, Lerp(compactWidth, ExpandedWidth, t));
        Notch.Height = Math.Max(0, Lerp(CompactHeight, ExpandedHeight, t));

        double radius = Math.Max(0, Lerp(CompactRadius, ExpandedRadius, t));
        Notch.CornerRadius = new CornerRadius(0, 0, radius, radius);

        // Konten expanded muncul setelah notch separuh jalan; compact memudar saat mulai melebar.
        // Keduanya disembunyikan selama animasi air.
        double expandedOpacity = _introActive ? 0 : Math.Clamp((t - 0.55) / 0.45, 0, 1);
        ExpandedContent.Opacity = expandedOpacity;
        ExpandedContent.Visibility = expandedOpacity > 0 ? Visibility.Visible : Visibility.Hidden;

        CompactContent.Opacity = _introActive ? 0 : Math.Clamp(1 - t / 0.35, 0, 1);

        Water.BottomRadius = radius;

    }

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;

}