public enum ActivityPriority { Low, Medium, High }

public enum ActivityIcon { None, Bolt, BatteryAlert, Timer, Music }

public enum ActivityTone { Neutral, Blue, Green, Yellow, Red }

namespace DynamicIsland.Core.Activities
{
    /// <summary>Isi satu sisi compact (kiri atau kanan).</summary>
    public sealed record ActivitySlot(
        ActivityIcon Icon = ActivityIcon.None,
        string? Text = null,
        ActivityTone Tone = ActivityTone.Neutral);

    /// <summary>Satu aktivitas yang ingin tampil di compact state.</summary>
    public sealed record LiveActivity(
        string Id,
        ActivityPriority Priority,
        ActivitySlot? Left,
        ActivitySlot? Right,
        TimeSpan? Duration = null
        );
}
