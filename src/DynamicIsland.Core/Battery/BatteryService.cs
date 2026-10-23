using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DynamicIsland.Core.Battery
{
    /// <summary>Snapshot kondisi baterai.</summary>
    public readonly record struct BatteryState(int Percent, bool IsCharging, bool HasBattery);

    public sealed class BatteryService: IDisposable
    {
        public BatteryState Current { get; private set;  }
        public event EventHandler<BatteryState>? Changed;

        public BatteryService()
        {
            Current = Read();
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.StatusChange) return;

            var next = Read();
            if (next == Current) return;

            Current = next;
            Changed?.Invoke(this,Current);
        }

        private static BatteryState Read()
        {
            if (!GetSystemPowerStatus(out var s))
                return new BatteryState(100, false, false);

            bool hasBattery = s.BatteryFlag != 128 && s.BatteryFlag != 255;
            int percent = s.BatteryLifePercent == 255 ? 100 : s.BatteryLifePercent;
            
            bool charging = s.ACLineStatus == 1;

            return new BatteryState(percent, charging, hasBattery);
        }
        public void Dispose() => SystemEvents.PowerModeChanged -= OnPowerModeChanged;

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);
    }
}
