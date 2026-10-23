using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace DynamicIsland.App.Controls
{
    /// <summary>Menerjemahkan enum dari Core menjadi gambar dan warna WPF.</summary>
    internal static class ActivityVisuals
    {
        // Ikon bergaya Material Design Icons (Apache 2.0), kotak 24x24.
        private static readonly Dictionary<ActivityIcon, Geometry> Icons = new()
        {
            [ActivityIcon.Bolt] =
                Make("M13,2 L4,14 L11,14 L10,22 L20,9 L13,9 Z"),
            [ActivityIcon.Music] =
                Make("M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z"),
            [ActivityIcon.Timer] =
                Make("M15 1H9v2h6V1zm-4 13h2V8h-2v6zm8.03-6.61l1.42-1.42c-.43-.51-.9-.99-1.41-1.41l-1.42 1.42C16.07 4.74 14.12 4 12 4c-4.97 0-9 4.03-9 9s4.02 9 9 9 9-4.03 9-9c0-2.12-.74-4.07-1.97-5.61zM12 20c-3.87 0-7-3.13-7-7s3.13-7 7-7 7 3.13 7 7-3.13 7-7 7z"),
            [ActivityIcon.BatteryAlert] =
                Make("M15.67 4H14V2h-4v2H8.33C7.6 4 7 4.6 7 5.33v15.33C7 21.4 7.6 22 8.33 22h7.33c.74 0 1.34-.6 1.34-1.33V5.33C17 4.6 16.4 4 15.67 4zM13 18h-2v-2h2v2zm0-4h-2V9h2v5z"),
        };

        private static readonly Dictionary<ActivityTone, Brush> Tones = new()
        {
            [ActivityTone.Neutral] = Solid(0xFF, 0xFF, 0xFF),
            [ActivityTone.Blue] = Solid(0x0A, 0x84, 0xFF),
            [ActivityTone.Green] = Solid(0x30, 0xD1, 0x58),
            [ActivityTone.Yellow] = Solid(0xFF, 0xD6, 0x0A),
            [ActivityTone.Red] = Solid(0xFF, 0x45, 0x3A),
        };

        public static Geometry? GetIcon(ActivityIcon icon) => Icons.GetValueOrDefault(icon);
        public static Brush GetBrush(ActivityTone tone) => Tones[tone];

        private static Geometry Make(string path)
        {
            var g = Geometry.Parse(path);
            g.Freeze();
            return g;
        }

        private static Brush Solid(byte r, byte g, byte b)
        {
            var br = new SolidColorBrush(Color.FromRgb(r, g, b));
            br.Freeze();
            return br;
        }
    }
}
