using System;
using System.Collections.Generic;
using System.Text;

namespace DynamicIsland.Core.Activities
{
    /// <summary>
    /// Menyimpan semua aktivitas aktif dan menentukan mana yang tampil:
    /// prioritas tertinggi menang; kalau sama, yang terbaru menang.
    /// </summary>
    public sealed class ActivityManager : IDisposable
    {
        private sealed class Entry
        {
            public required LiveActivity Activity { get; init; }
            public required long Seq { get; init; }
            public System.Threading.Timer? Timer { get; set; }
        }

        private readonly object _gate = new();
        private readonly List<Entry> _entries = new();
        private long _seq;

        public LiveActivity? Current { get; private set; }

        /// <summary>Bisa terpicu dari thread lain (timer). UI wajib memindahkan ke Dispatcher.</summary>
        public event EventHandler? CurrentChanged;

        public void Show(LiveActivity activity)
        {
            lock (_gate)
            {
                var old = _entries.Find(e => e.Activity.Id == activity.Id);
                if (old is not null)
                {
                    old.Timer?.Dispose();
                    _entries.Remove(old);
                }

                long seq = ++_seq;
                var entry = new Entry { Activity = activity, Seq = seq };

                // Timer sekali jalan, hanya ada selama aktivitas sementara masih hidup.
                if (activity.Duration is { } d)
                {
                    entry.Timer = new System.Threading.Timer(
                        _ => RemoveInternal(activity.Id, seq), null, d, Timeout.InfiniteTimeSpan);
                }

                _entries.Add(entry);
            }
            Recompute();
        }

        public void Remove(string id) => RemoveInternal(id, null);

        private void RemoveInternal(string id, long? onlySeq)
        {
            lock (_gate)
            {
                var e = _entries.Find(x => x.Activity.Id == id);
                // onlySeq mencegah timer lama menghapus aktivitas baru yang ber-id sama.
                if (e is null || (onlySeq is { } s && e.Seq != s)) return;

                e.Timer?.Dispose();
                _entries.Remove(e);
            }
            Recompute();
        }

        private void Recompute()
        {
            bool changed;
            lock (_gate)
            {
                var next = _entries
                    .OrderByDescending(e => e.Activity.Priority)
                    .ThenByDescending(e => e.Seq)
                    .FirstOrDefault()?.Activity;

                changed = !Equals(next, Current); // record: dibandingkan berdasarkan nilai
                Current = next;
            }
            if (changed) CurrentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            lock (_gate)
            {
                foreach (var e in _entries) e.Timer?.Dispose();
                _entries.Clear();
            }
        }
    }
}
