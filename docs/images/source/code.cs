using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NotNarakaLauncher.App.Interfaces;
public class HolidayTheme : IThemeEffect
{
    private Canvas _canvas;
    private List<(FrameworkElement flake, double speed, double drift)> _particles = new();
    private Random _rnd = new();
    private RadialGradientBrush _goldBrush;
    private RadialGradientBrush _redBrush;
    private DateTime _lastUpdate = DateTime.Now;
    private bool _isRunning = false;
    private EventHandler _renderHandler;
    public HolidayTheme()
    {
        _renderHandler = OnRendering;
    }
    public void Start(Canvas canvas, ResourceDictionary resources)
    {
        _canvas = canvas;
        _isRunning = true;
        _particles.Clear();
        // Safe Brush Creation
        Color gold = Colors.Gold;
        Color red = Colors.Red;
        if (resources.Contains("HolidayGold")) gold = (Color)resources["HolidayGold"];
        if (resources.Contains("HolidayRed")) red = (Color)resources["HolidayRed"];
        _goldBrush = CreateFrozenBrush(gold);
        _redBrush = CreateFrozenBrush(red);
        CompositionTarget.Rendering -= _renderHandler; // Safety
        CompositionTarget.Rendering += _renderHandler;
        // Initial Pop
        for (int i = 0; i < 60; i++) 
            SpawnParticle(randomY: true);
    }
    public void Stop()
    {
        _isRunning = false;
        CompositionTarget.Rendering -= _renderHandler;
        
        if (_canvas != null)
        {
            _canvas.Children.Clear();
            _canvas = null;
        }
        _particles.Clear();
    }
    private RadialGradientBrush CreateFrozenBrush(Color c)
    {
        var b = new RadialGradientBrush();
        b.GradientStops.Add(new GradientStop(c, 0.0));
        b.GradientStops.Add(new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1.0));
        b.Freeze();
        return b;
    }
    private void OnRendering(object sender, EventArgs e)
    {
        if (!_isRunning || _canvas == null) return;
        var now = DateTime.Now;
        if ((now - _lastUpdate).TotalMilliseconds < 16) return;
        _lastUpdate = now;
        UpdateParticles();
    }
    private void SpawnParticle(bool randomY = false)
    {
        if (_canvas == null) return;
        double size = _rnd.NextDouble() * 4.0 + 2.0; 
        var brush = _rnd.NextDouble() > 0.6 ? _goldBrush : _redBrush;
        var flake = new Ellipse
        {
            Width = size,
            Height = size,
            Opacity = _rnd.NextDouble() * 0.6 + 0.4,
            Fill = brush,
            IsHitTestVisible = false
        };
        double w = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : 1200;
        double h = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : 800;
        double x = _rnd.NextDouble() * w;
        double y = randomY ? _rnd.NextDouble() * h : -10;
        
        double speed = _rnd.NextDouble() * 1.5 + 0.5; 
        double drift = (_rnd.NextDouble() - 0.5) * 0.8;
        
        Canvas.SetLeft(flake, x);
        Canvas.SetTop(flake, y);
        _canvas.Children.Add(flake);
        _particles.Add((flake, speed, drift));
    }
    private void UpdateParticles()
    {
        if (_canvas == null) return;
        double maxY = _canvas.ActualHeight > 10 ? _canvas.ActualHeight + 20 : 820;
        double maxX = _canvas.ActualWidth > 10 ? _canvas.ActualWidth : 1200;
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var (flake, speed, drift) = _particles[i];
            double y = Canvas.GetTop(flake) + speed;
            double x = Canvas.GetLeft(flake) + drift;
            
            if (y > maxY || x < -10 || x > maxX + 10)
            {
                // Reset
                Canvas.SetTop(flake, -10);
                Canvas.SetLeft(flake, _rnd.NextDouble() * maxX);
            }
            else
            {
                Canvas.SetTop(flake, y);
                Canvas.SetLeft(flake, x);
            }
        }
        
        // Refill logic
        if (_particles.Count < 60 && _rnd.Next(10) == 0)
        {
            SpawnParticle();
        }
    }
}
return new HolidayTheme();
