using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DynamicIsland.App.Animation;
using DynamicIsland.App.Shell;

namespace DynamicIsland.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const double CompactWidth = 180;
    private const double CompactHeight= 32;
    private const double CompactRadius= 16;
    private const double ExpandedWidth = 420;
    private const double ExpandedHeight = 120;
    private const double ExpandedRadius = 32;

    private readonly SpringAnimator _spring = new SpringAnimator(initial: 0);
    private TimeSpan _lastRenderTime;
    private bool _isAnimating;

    public MainWindow()
    {
        InitializeComponent();
        
        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = 0;

        ApplyProgress(0);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowStyleHelper.MakeOverlay(new WindowInteropHelper(this).Handle);
    }

    private void NotchMouseEnter(object sender, MouseEventArgs e) => AnimateTo(1);
    private void NotchMouseLeave(object sender, MouseEventArgs e) => AnimateTo(0);

    private void AnimateTo(double target)
    {
        _spring.Target = target;
        if (_isAnimating) return;

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
        ApplyProgress(_spring.Value);

        if (_spring.IsSettled)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isAnimating = false;
        }
    }

    private void ApplyProgress(double t)
    {
        Notch.Width = Math.Max(0, Lerp(CompactWidth, ExpandedWidth, t));
        Notch.Height = Math.Max(0, Lerp(CompactHeight, ExpandedHeight, t));

        double radius = Math.Max(0, Lerp(CompactRadius, ExpandedRadius, t));
        Notch.CornerRadius = new CornerRadius(0, 0, radius, radius);
    }

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;

}