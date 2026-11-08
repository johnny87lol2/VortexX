using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VortexX.Services
{
    /// <summary>互動動畫（懸停放大、按下縮小）。可在設定裡整個關掉。</summary>
    public static class Motion
    {
        public static bool Enabled = true;
        static bool _init;

        public static void Init()
        {
            if (_init) return;
            _init = true;
            foreach (System.Type t in new System.Type[] { typeof(ButtonBase), typeof(Border) })
            {
                EventManager.RegisterClassHandler(t, UIElement.MouseEnterEvent, new MouseEventHandler((s, e) => Scale(s, 1.04, 140)));
                EventManager.RegisterClassHandler(t, UIElement.MouseLeaveEvent, new MouseEventHandler((s, e) => Scale(s, 1.0, 160)));
                EventManager.RegisterClassHandler(t, UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler((s, e) => Scale(s, 0.95, 80)));
                EventManager.RegisterClassHandler(t, UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler((s, e) => Scale(s, 1.04, 120)));
            }
        }

        static void Scale(object sender, double to, int ms)
        {
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null) return;
            if (fe is CheckBox || fe is RadioButton) return;
            if (fe is Border && !object.Equals(fe.Cursor, Cursors.Hand)) return;
            if (fe.Name == "BtnMin" || fe.Name == "BtnMax" || fe.Name == "BtnClose") return;

            ScaleTransform st = fe.RenderTransform as ScaleTransform;
            if (!Enabled)
            {
                if (st != null && !st.IsFrozen)
                {
                    st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    st.ScaleX = 1; st.ScaleY = 1;
                }
                return;
            }
            if (st == null || st.IsFrozen)
            {
                st = new ScaleTransform(1, 1);
                fe.RenderTransform = st;
                fe.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            var a = new DoubleAnimation(to, System.TimeSpan.FromMilliseconds(ms));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }
    }
}
