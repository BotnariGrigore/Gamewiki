using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace GameWikiApp.Helpers
{
    public static class UiAnimation
    {
        private static readonly ConcurrentDictionary<Control, CancellationTokenSource> _tokens = new();

        public static async Task ScaleToAsync(Control control, double target, int durationMs = 150)
        {
            if (control == null) return;

            if (_tokens.TryGetValue(control, out var existing))
            {
                try { existing.Cancel(); } catch { }
                try { existing.Dispose(); } catch { }
            }

            var cts = new CancellationTokenSource();
            _tokens[control] = cts;
            var ct = cts.Token;

            try
            {
                ScaleTransform transform = null;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    transform = control.RenderTransform as ScaleTransform;
                    if (transform == null)
                    {
                        transform = new ScaleTransform(1.0, 1.0);
                        control.RenderTransform = transform;
                    }
                });

                double from = 1.0;
                await Dispatcher.UIThread.InvokeAsync(() => { from = transform.ScaleX; });

                const int frameMs = 16;
                int steps = Math.Max(1, durationMs / frameMs);

                for (var i = 1; i <= steps; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    double t = (double)i / steps;
                    double value = from + (target - from) * t;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        transform.ScaleX = value;
                        transform.ScaleY = value;
                    });

                    await Task.Delay(frameMs, ct).ConfigureAwait(false);
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    transform.ScaleX = target;
                    transform.ScaleY = target;
                });
            }
            catch (OperationCanceledException) { }
            finally
            {
                _tokens.TryRemove(control, out _);
                try { cts.Dispose(); } catch { }
            }
        }

        public static async Task OpacityToAsync(Control control, double target, int durationMs = 150)
        {
            if (control == null) return;

            if (_tokens.TryGetValue(control, out var existing))
            {
                try { existing.Cancel(); } catch { }
                try { existing.Dispose(); } catch { }
            }

            var cts = new CancellationTokenSource();
            _tokens[control] = cts;
            var ct = cts.Token;

            try
            {
                double from = control.Opacity;
                const int frameMs = 16;
                int steps = Math.Max(1, durationMs / frameMs);

                for (var i = 1; i <= steps; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    double t = (double)i / steps;
                    double value = EaseInOutCubic(t) * (target - from) + from;
                    await Dispatcher.UIThread.InvokeAsync(() => control.Opacity = value);
                    await Task.Delay(frameMs, ct);
                }

                await Dispatcher.UIThread.InvokeAsync(() => control.Opacity = target);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _tokens.TryRemove(control, out _);
                try { cts.Dispose(); } catch { }
            }
        }

        public static async Task BackgroundColorToAsync(Border control, Color targetColor, int durationMs = 150)
        {
            if (control == null) return;

            if (_tokens.TryGetValue(control, out var existing))
            {
                try { existing.Cancel(); } catch { }
                try { existing.Dispose(); } catch { }
            }

            var cts = new CancellationTokenSource();
            _tokens[control] = cts;
            var ct = cts.Token;

            try
            {
                // Determine starting color
                Color fromColor = Colors.Transparent;
                if (control.Background is SolidColorBrush sb)
                {
                    fromColor = sb.Color;
                }

                const int frameMs = 16;
                int steps = Math.Max(1, durationMs / frameMs);

                for (var i = 1; i <= steps; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    double t = (double)i / steps;
                    double eased = EaseInOutCubic(t);

                    byte a = (byte)(fromColor.A + (targetColor.A - fromColor.A) * eased);
                    byte r = (byte)(fromColor.R + (targetColor.R - fromColor.R) * eased);
                    byte g = (byte)(fromColor.G + (targetColor.G - fromColor.G) * eased);
                    byte b = (byte)(fromColor.B + (targetColor.B - fromColor.B) * eased);

                    var color = Color.FromArgb(a, r, g, b);
                    await Dispatcher.UIThread.InvokeAsync(() => control.Background = new SolidColorBrush(color));
                    await Task.Delay(frameMs, ct);
                }

                await Dispatcher.UIThread.InvokeAsync(() => control.Background = new SolidColorBrush(targetColor));
            }
            catch (OperationCanceledException) { }
            finally
            {
                _tokens.TryRemove(control, out _);
                try { cts.Dispose(); } catch { }
            }
        }

        private static double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }
    }
}
