using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DynamicIsland.App.Controls
{
    /// <summary>Ring progress baterai dengan ikon petir di tengah.</summary>
    public sealed class BatteryRing: FrameworkElement
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register(nameof(Progress), typeof(double), typeof(BatteryRing),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsChargingProperty =
            DependencyProperty.Register(nameof(IsCharging), typeof(bool), typeof(BatteryRing),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty); 
            set => SetValue(ProgressProperty, value);
        }
    
        public bool IsCharging
        {
            get => (bool)GetValue(IsChargingProperty); 
            set => SetValue(IsChargingProperty, value);
        }

        private static readonly Brush Blue = Make(0x0A, 0x84, 0xFF);
        private static readonly Brush Green = Make(0x30, 0xD1, 0x58);
        private static readonly Brush Track = Make(0x3A, 0x3A, 0x3C);
        private static readonly Brush DarkBolt = Make(0x48, 0x48, 0x4A);

        // Petir digambar di kotak 24x24, nanti diskalakan.
        private static readonly Geometry Bolt = Freeze(Geometry.Parse("M13,2 L4,14 L11,14 L10,22 L20,9 L13,9 Z"));

        private const double Thickness = 4;

        protected override void OnRender(DrawingContext dc)
        {
            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= 0) return;

            var center = new Point(ActualWidth / 2, ActualHeight / 2);
            double radius = (size - Thickness) / 2;
            Brush accent = IsCharging ? Green : Blue;

            dc.DrawEllipse(null, new Pen(Track, Thickness), center, radius, radius);
            double p = Math.Clamp(Progress, 0, 1);
            if (p >= 0.999)
            {
                dc.DrawEllipse(null, new Pen(accent, Thickness), center, radius, radius);
            }
            else if (p > 0.001)
            {
                double angle = 2 * Math.PI * p;
                var start = new Point(center.X, center.Y - radius);
                var end = new Point(center.X + radius * Math.Sin(angle),
                                    center.Y - radius * Math.Cos(angle));

                var arc = new StreamGeometry();
                using (var ctx = arc.Open())
                {
                    ctx.BeginFigure(start, false, false);
                    ctx.ArcTo(end, new Size(radius, radius), 0, p > 0.5,
                              SweepDirection.Clockwise, true, false);
                }
                arc.Freeze();

                var pen = new Pen(accent, Thickness)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
                dc.DrawGeometry(null, pen, arc);
            }
            double scale = size * 0.5 / 24;
            dc.PushTransform(new TranslateTransform(center.X - 12 * scale, center.Y - 12 * scale));
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.DrawGeometry(IsCharging ? Green : DarkBolt, null, Bolt);
            dc.Pop();
            dc.Pop();
        }

        /// <summary>Set level langsung (tanpa animasi) atau dengan transisi halus.</summary>
        public void SetLevel(double percent, bool animate = true)
        {
            double target = Math.Clamp(percent, 0, 100) / 100.0;

            if (!animate)
            {
                BeginAnimation(ProgressProperty, null); // lepas animasi lama
                Progress = target;
                return;
            }

            BeginAnimation(ProgressProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(500))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        /// <summary>Animasi saat charging dimulai: penuh dulu, lalu turun ke persentase asli.</summary>


        public void PlayChargeIntro(double percent)
        {
            double target = Math.Clamp(percent, 0, 100) / 100.0;

            var anim = new DoubleAnimationUsingKeyFrames();
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(target,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1500)),
                new CubicEase { EasingMode = EasingMode.EaseInOut }));

            BeginAnimation(ProgressProperty, anim);
        }
        private static Brush Make(byte r, byte g, byte b) => Freeze(new SolidColorBrush(Color.FromRgb(r, g, b)));

        private static T Freeze<T>(T f) where T : Freezable
        {
            f.Freeze();
            return f;
        }
    }
}
