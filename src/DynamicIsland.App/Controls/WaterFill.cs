using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace DynamicIsland.App.Controls
{
    /// <summary>
    /// Animasi air hijau bergelombang yang naik dari bawah.
    /// Mengatur waktunya sendiri, dan melepas loop render saat selesai.
    /// </summary>
    public sealed class WaterFill : FrameworkElement
    {
        // ----- Pengaturan waktu (detik) dan level; ubah di sini untuk menyetel -----
        private const double RiseToHalf = 0.8;
        private const double Hold = 0.5;
        private const double RiseToFull = 0.7;
        private const double HalfLevel = 0.5;
        private const double WaveSpeed = 5.0;

        public static readonly DependencyProperty BottomRadiusProperty =
            DependencyProperty.Register(nameof(BottomRadius), typeof(double), typeof(WaterFill),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double BottomRadius
        {
            get => (double)GetValue(BottomRadiusProperty);
            set => SetValue(BottomRadiusProperty, value);
        }

        /// <summary>Terpicu saat layar sudah hijau penuh.</summary>
        public event EventHandler? Filled;

        private static readonly Brush Front = Make(0x30, 0xD1, 0x58, 0xFF);
        private static readonly Brush Back = Make(0x30, 0xD1, 0x58, 0x90);

        private double _elapsed;
        private double _phase;
        private double _level;
        private TimeSpan _last;
        private bool _running;

        public void Start()
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = 1;
            Visibility = Visibility.Visible;

            _elapsed = 0;
            _phase = 0;
            _level = 0;
            _last = TimeSpan.Zero;

            if (!_running)
            {
                _running = true;
                CompositionTarget.Rendering += OnRendering;
            }
            InvalidateVisual();
        }

        public void Cancel()
        {
            if (_running)
            {
                CompositionTarget.Rendering -= OnRendering;
                _running = false;
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            var now = ((RenderingEventArgs)e).RenderingTime;
            double dt = _last == TimeSpan.Zero ? 1.0 / 60 : (now - _last).TotalSeconds;
            if (dt <= 0) return;
            _last = now;
            dt = Math.Min(dt, 0.05);

            _elapsed += dt;
            _phase += dt * WaveSpeed;
            _level = LevelAt(_elapsed);

            InvalidateVisual();
            if (_elapsed >= RiseToHalf + Hold + RiseToFull)
            {
                _level = 1;
                Cancel(); // berhenti menggambar ulang; hijau penuh tetap tampil
                Filled?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Level air (0..1) pada waktu t sejak animasi dimulai.</summary>
        private static double LevelAt(double t)
        {
            if (t < RiseToHalf)
                return HalfLevel * EaseOutCubic(t / RiseToHalf);

            t -= RiseToHalf;
            if (t < Hold)
                return HalfLevel;
            t -= Hold;
            double p = Math.Min(t / RiseToFull, 1);
            return HalfLevel + (1 - HalfLevel) * EaseInOutCubic(p);
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            // Potong semua gambar mengikuti bentuk notch (sudut bawah membulat).
            dc.PushClip(BuildClip(w, h, BottomRadius));

            // Tinggi gelombang menyesuaikan ukuran notch (kecil di compact).
            double amp = Math.Clamp(h * 0.05, 2, 8);
            double wavelength = Math.Max(60, w / 1.6);

            // Gelombang belakang dulu (lebih transparan, fase berbeda), lalu depan.
            DrawWave(dc, w, h, _level, amp * 1.2, _phase * 0.8 + 1.6, wavelength * 1.3, Back);
            DrawWave(dc, w, h, _level, amp, _phase, wavelength, Front);

            dc.Pop();
        }

        private static void DrawWave(DrawingContext dc, double w, double h, double level,
                             double amp, double phase, double wavelength, Brush brush)
        {
            // Pada level 1, seluruh puncak gelombang sudah di atas tepi atas = hijau penuh.
            double top = Lerp(h + amp, -amp, level);
            int n = Math.Max(8, (int)(w / 4));

            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new Point(0, h), true, true);
                for (int i = 0; i <= n; i++)
                {
                    double x = w * i / n;
                    double y = top + amp * Math.Sin(2 * Math.PI * x / wavelength + phase);
                    ctx.LineTo(new Point(x, y), true, false);
                }
                ctx.LineTo(new Point(w, h), true, false);
            }
            g.Freeze();
            dc.DrawGeometry(brush, null, g);
        }

        private static Geometry BuildClip(double w, double h, double r)
        {
            r = Math.Clamp(r, 0, Math.Min(w / 2, h));
            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new Point(0, 0), true, true);
                ctx.LineTo(new Point(w, 0), true, false);
                ctx.LineTo(new Point(w, h - r), true, false);
                ctx.ArcTo(new Point(w - r, h), new Size(r, r), 0, false, SweepDirection.Clockwise, true, false);
                ctx.LineTo(new Point(r, h), true, false);
                ctx.ArcTo(new Point(0, h - r), new Size(r, r), 0, false, SweepDirection.Clockwise, true, false);
            }
            g.Freeze();
            return g;
        }

        private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);
        private static double EaseInOutCubic(double t) =>
            t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static Brush Make(byte r, byte g, byte b, byte a)
        {
            var br = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            br.Freeze();
            return br;
        }
    }
}
