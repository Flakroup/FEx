using FEx.Agnostics.Abstractions.Extensions;
using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SuppressMessage = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace FEx.WPFx.Controls;

public class ZoomBorder : Border, INotifyPropertyChanged
{
    private readonly ScaleTransform _st = new();
    private readonly TranslateTransform _tt = new();
    private readonly Duration _zoomAnimationDuration = new(TimeSpan.FromMilliseconds(100));
    private readonly SemaphoreSlim _zoomSemaphore;

    private UIElement _child;
    private bool _isReseted;
    private Point _originBottomRight;
    private Point _originTopLeft;
    private Point _start;
    private bool _zoomingEnabled;

    private bool _isAnimationCancelled = true;
    private double _scale;
    private bool _isAutoFitEnabled;

    public event PropertyChangedEventHandler PropertyChanged;

    public override UIElement Child
    {
        get => base.Child;
        set
        {
            if (value is not null
                && !Equals(value, Child))
                Initialize(value);

            base.Child = value;
        }
    }

    public double MaxScale { get; } = 3;
    public double MaxScaleToZoomIn { get; } = 2;
    public double DefaultScale { get; } = 1;
    public double DefaultZoom { get; } = .1;
    public Action<ZoomBorder, double> ScaleChanged { get; private set; }

    public ConcurrentDictionary<string, IDisposable> Subscriptions { get; }

    public double Scale
    {
        get => _scale;
        set => this.SetObjectProperty(ref _scale, value, (z, _, _) => Task.Run(() => ScaleChanged?.Invoke(z, Scale)));
    }

    public bool IsAutoFitEnabled
    {
        get => _isAutoFitEnabled;
        set => this.SetObjectProperty(ref _isAutoFitEnabled, value, (_, _, v) => OnAutoFitChanged(v));
    }

    protected Size ParentSize { get; set; }

    protected TaskCompletionSource<bool> AnimationSXTcs { get; set; }
    protected TaskCompletionSource<bool> AnimationSYTcs { get; set; }
    protected TaskCompletionSource<bool> AnimationAXTcs { get; set; }
    protected TaskCompletionSource<bool> AnimationAYTcs { get; set; }

    public ZoomBorder()
    {
        Subscriptions = new();
        _zoomSemaphore = new(1, 1);

        KeyDown += ZoomBorder_KeyDown;
        KeyUp += ZoomBorder_KeyUp;
        MouseWheel += ZoomBorder_MouseWheel;
        MouseLeftButtonDown += ZoomBorder_MouseLeftButtonDown;
        MouseLeftButtonUp += ZoomBorder_MouseLeftButtonUp;
        SizeChanged += ZoomBorder_SizeChanged;
        MouseMove += ZoomBorder_MouseMove;
        PreviewMouseDown += ZoomBorder_PreviewMouseButtonDown;
        Scale = DefaultScale;
    }

    public void SetOnScaleChanged(Action<ZoomBorder, double> handler) => ScaleChanged = handler;

    public void Reset()
    {
        if (!_isReseted
            && _child is not null)
        {
            CancelPreviousAnimation();
            _st.ScaleX = 1.0;
            _st.ScaleY = 1.0;

            _tt.X = 0.0;
            _tt.Y = 0.0;
            _isReseted = true;
            Scale = DefaultScale;
        }
    }

    public void Zoom(double zoom, Point position, bool animate)
    {
        if (_child is not null)
        {
            double newScale = _st.ScaleX + zoom;

            if (newScale < DefaultScale)
                zoom = DefaultScale - _st.ScaleX;
            else if (newScale > MaxScale)
                zoom = MaxScale - _st.ScaleX;

            if (!zoom.Equals(0)
                && Scale != _st.ScaleX + zoom)
                ApplyTransform(zoom, position, animate);
        }
    }

    public async Task WaitForAnimationToCompleteAsync() => await Task.WhenAll(AnimationSXTcs?.Task ?? Task.CompletedTask,
            AnimationSYTcs?.Task ?? Task.CompletedTask,
            AnimationAXTcs?.Task ?? Task.CompletedTask,
            AnimationAYTcs?.Task ?? Task.CompletedTask);

    public void OnAutoFitChanged(bool autoFit)
    {
        if (!autoFit)
        {
            Reset();
        }
        else
        {
            double zoom = GetZoom();

            if (zoom != Math.Round(Scale - 1, 2))
            {
                if (!_isReseted)
                {
                    Reset();
                    zoom = GetZoom();
                }

                if (zoom != Math.Round(Scale - 1, 2))
                {
                    double x = _child.RenderSize.Width / 2 - 2;
                    double y = _child.RenderSize.Height / 2 - 2;
                    ApplyTransform(zoom, new(x, y), false);
                }
            }
        }
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        var parent = (FrameworkElement)Parent;
        ParentSize = new(parent.ActualWidth, parent.ActualHeight);
    }

    private void Initialize(UIElement element)
    {
        _child = element;

        if (_child is not null)
        {
            var group = new TransformGroup();
            group.Children.Add(_st);
            group.Children.Add(_tt);
            _child.RenderTransform = group;
            _child.RenderTransformOrigin = new(0.0, 0.0);

            var childElement = _child as FrameworkElement;

            if (childElement is not null)
                childElement.SizeChanged += Element_SizeChanged;
        }
    }

    private void ZoomBorder_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LeftCtrl or Key.RightCtrl:
                _zoomingEnabled = true;

                break;
            case Key.Down or Key.Up:
            {
                CancelPreviousAnimation();
                SetCoordinates();
                _start = TranslatePoint(new(0.0, 0.0), null);

                Point currentPosition = e.Key == Key.Down
                    ? new(_start.X,
                        new[]
                        {
                            _start.Y - 100D * (_st.ScaleY - 1),
                            _start.Y - (_originBottomRight.Y - ParentSize.Height)
                        }.Max())
                    : new Point(_start.X,
                        new[] { _start.Y + 100D * (_st.ScaleY - 1), Math.Abs(_originTopLeft.Y) }.Min());

                MoveChild(currentPosition);
                ApplyTransform(0, _start);
                e.Handled = true;

                break;
            }
        }
    }

    private void SetCoordinates()
    {
        _originTopLeft = new(_tt.X, _tt.Y);

        _originBottomRight = new(_tt.X + _child.RenderSize.Width * _st.ScaleX,
            _tt.Y + _child.RenderSize.Height * _st.ScaleY);
    }

    private void ZoomBorder_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl or Key.RightCtrl)
            _zoomingEnabled = false;
    }

    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods")]
    private async void ZoomBorder_MouseWheel(object sender, [NotNull] MouseWheelEventArgs e)
    {
        if (_zoomingEnabled)
        {
            await _zoomSemaphore.WaitAsync();

            try
            {
                await WaitForAnimationToCompleteAsync();

                Zoom(e.Delta > 0
                        ? DefaultZoom
                        : -DefaultZoom,
                    e,
                    false);

                await WaitForAnimationToCompleteAsync();
            }
            finally
            {
                _zoomSemaphore.Release();
            }
        }
    }

    private void ZoomBorder_MouseLeftButtonDown(object sender, [NotNull] MouseButtonEventArgs e)
    {
        if (_child is not null)
        {
            if (e.ClickCount >= 2)
            {
                Zoom(_st.ScaleX < MaxScaleToZoomIn
                        ? MaxScale - _st.ScaleX
                        : -_st.ScaleX + DefaultScale,
                    e,
                    true);
            }
            else
            {
                CancelPreviousAnimation();
                _start = e.GetPosition(this);
                SetCoordinates();
                Cursor = Cursors.Hand;
                _child.CaptureMouse();
            }
        }
    }

    private void ZoomBorder_MouseLeftButtonUp(object sender, [NotNull] MouseButtonEventArgs e)
    {
        if (_child is not null
            && e.LeftButton != MouseButtonState.Pressed)
        {
            _child.ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
        }
    }

    private void ZoomBorder_PreviewMouseButtonDown(object sender, [NotNull] MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
            Reset();
    }

    private void ZoomBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (_child?.IsMouseCaptured == true
            && e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPosition = e.GetPosition(this);
            MoveChild(currentPosition);
        }
    }

    private void MoveChild(Point currentPosition)
    {
        Vector v = _start - currentPosition;

        double newXLeft = _originTopLeft.X - v.X;
        double newXRight = _originBottomRight.X - v.X;
        double newYTop = _originTopLeft.Y - v.Y;
        double newYBottom = _originBottomRight.Y - v.Y;
        bool shiftX = true, shiftY = true;

        if (v.X < 0)
        {
            if (newXLeft > 0)
                shiftX = false;
        }
        else
        {
            if (newXRight < _child.RenderSize.Width)
                shiftX = false;
        }

        if (v.Y < 0)
        {
            if (newYTop > 0)
                shiftY = false;
        }
        else
        {
            if (newYBottom < _child.RenderSize.Height)
                shiftY = false;
        }

        if (shiftX || shiftY)
        {
            if (shiftX)
                _tt.X = newXLeft;

            if (shiftY)
                _tt.Y = newYTop;
        }
    }

    private void ZoomBorder_SizeChanged(object sender, [NotNull] SizeChangedEventArgs e)
    {
        const double tolerance = 0.1;

        if (!e.PreviousSize.Height.Equals(0)
            && Math.Abs(e.PreviousSize.Height - e.NewSize.Height) >= tolerance)
            OnAutoFitChanged(IsAutoFitEnabled);
    }

    private void Zoom(double zoom, MouseEventArgs e, bool animate) => Zoom(zoom,
            _child is not null
                ? e.GetPosition(_child)
                : default,
            animate);

    private void ApplyTransform(double zoom, Point position, bool animate = true)
    {
        (double absoluteX, double absoluteY, double newScaleX, double newScaleY, double diffX, double diffY) =
            GetNewCoordinates(zoom, position);

        ApplyTransform(zoom, animate, absoluteX, absoluteY, newScaleX, newScaleY, diffX, diffY);
    }

    private void ApplyTransform(double zoom,
                                bool animate,
                                double absoluteX,
                                double absoluteY,
                                double newScaleX,
                                double newScaleY,
                                double diffX,
                                double diffY)
    {
        double newX = absoluteX - diffX;
        double newY = absoluteY - diffY;

        if (zoom <= 0)
        {
            var bottomRight = new Point(absoluteX + _child.RenderSize.Width * newScaleX,
                absoluteY + _child.RenderSize.Height * newScaleY);

            double newXRight = bottomRight.X - diffX;
            double newYBottom = bottomRight.Y - diffY;

            if (newXRight < _child.RenderSize.Width)
                newX += _child.RenderSize.Width - newXRight;

            if (newYBottom < _child.RenderSize.Height)
                newY += _child.RenderSize.Height - newYBottom;

            newX = newX > 0
                ? 0
                : newX;

            newY = newY > 0
                ? 0
                : newY;
        }

        if (animate)
        {
            Animate(newScaleX, newScaleY, newX, newY);
        }
        else
        {
            CancelPreviousAnimation();
            _st.ScaleX = newScaleX;
            _st.ScaleY = newScaleY;
            _tt.X = newX;
            _tt.Y = newY;
        }

        Scale = newScaleX;
        _isReseted = false;
    }

    private (double absoluteX, double absoluteY, double newScaleX, double newScaleY, double diffX, double diffY)
        GetNewCoordinates(double zoom, Point position)
    {
        double absoluteX = position.X * _st.ScaleX + _tt.X;
        double absoluteY = position.Y * _st.ScaleY + _tt.Y;
        double newScaleX = _st.ScaleX + zoom;
        double newScaleY = _st.ScaleY + zoom;
        double diffX = position.X * newScaleX;
        double diffY = position.Y * newScaleY;

        return (absoluteX, absoluteY, newScaleX, newScaleY, diffX, diffY);
    }

    private void Animate(double newScaleX, double newScaleY, double newX, double newY)
    {
        DoubleAnimation scaleXAnimation = SetAnimation(_st.ScaleX, newScaleX, nameof(AnimationSXTcs));
        DoubleAnimation scaleYAnimation = SetAnimation(_st.ScaleY, newScaleY, nameof(AnimationSYTcs));
        DoubleAnimation aX = SetAnimation(_tt.X, newX, nameof(AnimationAXTcs));
        DoubleAnimation aY = SetAnimation(_tt.Y, newY, nameof(AnimationAYTcs));

        _isAnimationCancelled = false;
        _st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
        _st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        _tt.BeginAnimation(TranslateTransform.XProperty, aX);
        _tt.BeginAnimation(TranslateTransform.YProperty, aY);
    }

    private DoubleAnimation SetAnimation(double from, double to, string key)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = _zoomAnimationDuration
        };

        Subscriptions.ReplaceAndDisposeOldValue(key,
            () => Observable
                .FromEventPattern<EventHandler, EventArgs>(h => animation.Completed += h, h => animation.Completed -= h)
                .Subscribe(_ => OnCompletedAnimation(key)));

        switch (key)
        {
            case nameof(AnimationSXTcs):
                AnimationSXTcs = new();

                break;
            case nameof(AnimationSYTcs):
                AnimationSYTcs = new();

                break;
            case nameof(AnimationAXTcs):
                AnimationAXTcs = new();

                break;
            case nameof(AnimationAYTcs):
                AnimationAYTcs = new();

                break;
        }

        return animation;
    }

    private void OnCompletedAnimation(string key)
    {
        switch (key)
        {
            case nameof(AnimationSXTcs):
                AnimationSXTcs.TrySetResult(true);

                break;
            case nameof(AnimationSYTcs):
                AnimationSYTcs.TrySetResult(true);

                break;
            case nameof(AnimationAXTcs):
                AnimationAXTcs.TrySetResult(true);

                break;
            case nameof(AnimationAYTcs):
                AnimationAYTcs.TrySetResult(true);

                break;
        }
    }

    private void CancelPreviousAnimation()
    {
        if (!_isAnimationCancelled)
        {
            AnimationSXTcs.TrySetResult(false);
            AnimationSYTcs.TrySetResult(false);
            AnimationAXTcs.TrySetResult(false);
            AnimationAYTcs.TrySetResult(false);

            double x = _tt.X, y = _tt.Y, scaleX = _st.ScaleX, scaleY = _st.ScaleY;
            _st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _tt.BeginAnimation(TranslateTransform.XProperty, null);
            _tt.BeginAnimation(TranslateTransform.YProperty, null);
            _tt.X = x;
            _tt.Y = y;
            _st.ScaleX = scaleX;
            _st.ScaleY = scaleY;
            _isAnimationCancelled = true;
        }
    }

    private void Element_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsAutoFitEnabled)
            OnAutoFitChanged(true);
    }

    private double GetZoom()
    {
        Point relativePoint = TranslatePoint(new(0.0, 0.0), null);
        var originTopLeft = new Point(relativePoint.X, relativePoint.Y);
        var originBottomRight = new Point(relativePoint.X + RenderSize.Width, relativePoint.Y + RenderSize.Height);

        double zoom = Math.Round(Math.Min((originBottomRight.X - originTopLeft.X) / _child.RenderSize.Width,
                                     (originBottomRight.Y - originTopLeft.Y) / _child.RenderSize.Height)
                                 - 1,
            2);

        if (zoom > MaxScaleToZoomIn)
            zoom = MaxScaleToZoomIn;

        return zoom;
    }
}
