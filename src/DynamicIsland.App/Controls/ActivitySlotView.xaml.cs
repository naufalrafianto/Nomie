using DynamicIsland.Core.Activities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace DynamicIsland.App.Controls
{

    public partial class ActivitySlotView : UserControl
    {
        public ActivitySlotView()
        {
            InitializeComponent();
            Opacity = 0;
            Visibility = Visibility.Hidden;
        }

        public void Show(ActivitySlot slot)
        {
            var brush = ActivityVisuals.GetBrush(slot.Tone);
            bool hasIcon = slot.Icon != ActivityIcon.None;
            bool hasText = !string.IsNullOrEmpty(slot.Text);

            IconPath.Data = ActivityVisuals.GetIcon(slot.Icon);
            IconPath.Fill = brush;
            IconPath.Visibility = hasIcon ? Visibility.Visible : Visibility.Collapsed;

            LabelText.Text = slot.Text ?? "";
            LabelText.Foreground = brush;
            LabelText.Margin = hasIcon && hasText ? new Thickness(6, 0, 0, 0) : new Thickness(0);

            Visibility = Visibility.Visible;

            // Muncul sedikit terlambat supaya notch melebar dulu (stagger).
            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220))
            {
                BeginTime = TimeSpan.FromMilliseconds(80)
            });
        }

        public void Hide()
        {
            if (Visibility != Visibility.Visible) return;

            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
            fade.Completed += (_, _) =>
            {
                if (Opacity == 0) Visibility = Visibility.Hidden;
            };
            BeginAnimation(OpacityProperty, fade);
        }
    }
}
