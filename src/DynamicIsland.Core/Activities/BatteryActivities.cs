using DynamicIsland.Core.Battery;
using System;
using System.Collections.Generic;
using System.Text;

namespace DynamicIsland.Core.Activities
{
    /// <summary>Pabrik aktivitas yang berkaitan dengan baterai.</summary>
    public static class BatteryActivities
    {
        public const string ChargingId = "battery.charging";
        public const string LowId = "battery.low";

        /// <summary>Sementara: petir + persentase, hilang sendiri setelah 4 detik.</summary>
        public static LiveActivity Charging(int percent) => new(
            ChargingId, ActivityPriority.Medium,
            new ActivitySlot(ActivityIcon.Bolt, null, ActivityTone.Green),
            new ActivitySlot(ActivityIcon.None, $"{percent}%", ActivityTone.Green),
            TimeSpan.FromSeconds(4));

        /// <summary>Persisten selama baterai rendah dan tidak sedang dicolok. Null = tidak perlu.</summary>
        public static LiveActivity? LowBattery(BatteryState s)
        {
            if (!s.HasBattery || s.IsCharging || s.Percent > 20) return null;

            bool critical = s.Percent <= 10;
            var tone = critical ? ActivityTone.Red : ActivityTone.Yellow;

            return new LiveActivity(
                LowId,
                critical ? ActivityPriority.High : ActivityPriority.Medium,
                new ActivitySlot(ActivityIcon.BatteryAlert, null, tone),
                new ActivitySlot(ActivityIcon.None, $"{s.Percent}%", tone));
        }

    }
}