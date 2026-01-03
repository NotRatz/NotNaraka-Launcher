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
    private Random _rnd = new();
    private bool _isRunning = false;
    private EventHandler _renderHandler;
    
    // Fireworks
    private List<FireworkParticle> _particles = new();
    private double _timeSinceLastLaunch = 0;
    
    // 2026 Glimmer
    private List<Rectangle> _textPixels = new();
    private double _glimmerPhase = 0;
    private class FireworkParticle
    {
        public Ellipse Element;
        public double X, Y;
        public double VX, VY;
        public double Alpha;
        public bool IsRocket; // true = shooting up, false = exploded spark
        public Color Color;
    }
    public HolidayTheme()
    {
        _renderHandler = OnRendering;
    }
    public void Start(Canvas canvas, ResourceDictionary resources)
    {
        _canvas = canvas;
        _isRunning = true;
        _particles.Clear();
        _textPixels.Clear();
        CompositionTarget.Rendering -= _renderHandler;
        CompositionTarget.Rendering += _renderHandler;
        // Draw the static "2026" text immediately (if valid) or defer
        if (_canvas.ActualWidth > 0) DrawYear2026();
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
        _textPixels.Clear();
    }
    private void OnRendering(object sender, EventArgs e)
    {
        if (!_isRunning || _canvas == null) return;
        
        // Pause if window is minimized or not visible to save resources
        var win = Window.GetWindow(_canvas);
        if (win != null && (win.WindowState == WindowState.Minimized || !win.IsVisible)) return;
        // Ensure text is drawn if it wasn't valid at Start
        if (_textPixels.Count == 0 && _canvas.ActualWidth > 0)
        {
             DrawYear2026();
        }
        // 1. Update Fireworks
        UpdateFireworks();
        // 2. Glimmer the "2026" Text
        UpdateTextGlimmer();
    }
    // --- Fireworks Logic ---
    private void UpdateFireworks()
    {
        // Probability to launch (approx every 0.8s at 60fps)
        _timeSinceLastLaunch += 1.0;
        if (_timeSinceLastLaunch > 45 && _rnd.NextDouble() < 0.1) 
        {
            LaunchRocket();
            _timeSinceLastLaunch = 0;
        }
        double width = _canvas.ActualWidth;
        double height = _canvas.ActualHeight;
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            
            // Physics
            p.X += p.VX;
            p.Y += p.VY;
            if (p.IsRocket)
            {
                // Rocket Logic: Move up, slow down
                p.VY += 0.05; // Gravity drag
                
                // Explode at peak or random height
                if (p.VY >= -0.5 || p.Y < height * 0.2) 
                {
                    Explode(p);
                    RemoveParticle(i);
                    continue;
                }
            }
            else
            {
                // Spark Logic: Fall, fade
                p.VY += 0.1; // Gravity
                p.Alpha -= 0.015; // Fade out
                p.Element.Opacity = p.Alpha;
                // Remove if invisible or out of bounds
                if (p.Alpha <= 0 || p.Y > height) 
                {
                    RemoveParticle(i);
                    continue;
                }
            }
            // Render
            Canvas.SetLeft(p.Element, p.X);
            Canvas.SetTop(p.Element, p.Y);
        }
    }
    private void LaunchRocket()
    {
        double startX = _rnd.NextDouble() * _canvas.ActualWidth * 0.8 + (_canvas.ActualWidth * 0.1);
        double startY = _canvas.ActualHeight + 10;
        
        // Determine mode for colors
        bool isSplash = _canvas.ActualWidth < 1000;
        var color = GetRandomColor(isSplash);
        
        var particle = new FireworkParticle
        {
            X = startX,
            Y = startY,
            VX = (_rnd.NextDouble() - 0.5) * 1.0, // Slight drift
            VY = -(_rnd.NextDouble() * 5.0 + 8.0), // Fast launch speed
            IsRocket = true,
            Color = color,
            Alpha = 1.0,
            Element = CreateEllipse(4, color)
        };
        
        _particles.Add(particle);
        _canvas.Children.Add(particle.Element);
    }
    
    // ... Explode/RemoveParticle/CreateEllipse unchanged ...
    private Color GetRandomColor(bool isSplash)
    {
        if (isSplash)
        {
            // Green/White Monochrome theme for Splash
            return _rnd.NextDouble() > 0.5 ? Colors.White : Color.FromRgb(66, 255, 66);
        }
        
        // Vibrant 8-bit style colors for Main
        var colors = new[] 
        {
            Colors.Cyan, Colors.Magenta, Colors.Yellow, Colors.Lime, 
            Color.FromRgb(255, 100, 100) // Hot Pink/Red
        };
        return colors[_rnd.Next(colors.Length)];
    }
    private void Explode(FireworkParticle rocket)
    {
        // Spawn 30-50 sparks
        int count = _rnd.Next(30, 50);
        for (int i = 0; i < count; i++)
        {
            double angle = _rnd.NextDouble() * Math.PI * 2;
            double speed = _rnd.NextDouble() * 3.0 + 1.0;
            
            var spark = new FireworkParticle
            {
                X = rocket.X,
                Y = rocket.Y,
                VX = Math.Cos(angle) * speed,
                VY = Math.Sin(angle) * speed,
                IsRocket = false,
                Color = rocket.Color,
                Alpha = 1.0,
                Element = CreateEllipse(2, rocket.Color)
            };
            
            _particles.Add(spark);
            _canvas.Children.Add(spark.Element);
        }
    }
    private void RemoveParticle(int index)
    {
        if (index < 0 || index >= _particles.Count) return;
        _canvas.Children.Remove(_particles[index].Element);
        _particles.RemoveAt(index);
    }
    private Ellipse CreateEllipse(double size, Color color)
    {
        var e = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color),
            IsHitTestVisible = false
        };
        // Freeze brush for performance? 
        if (e.Fill.CanFreeze) e.Fill.Freeze();
        return e;
    }
    private Color GetRandomColor()
    {
        // Vibrant 8-bit style colors
        var colors = new[] 
        {
            Colors.Cyan, Colors.Magenta, Colors.Yellow, Colors.Lime, 
            Color.FromRgb(255, 100, 100) // Hot Pink/Red
        };
        return colors[_rnd.Next(colors.Length)];
    }
    // --- 8-Bit Text Logic ---
    private void DrawYear2026()
    {
        // 5x3 Grid Masks
        string[] digit2 = { "111", "001", "111", "100", "111" };
        string[] digit0 = { "111", "101", "101", "101", "111" };
        string[] digit6 = { "111", "100", "111", "101", "111" };
        double screenWidth = _canvas.ActualWidth;
        double screenHeight = _canvas.ActualHeight;
        // Scaling Factor based on screen size
        bool isSplash = screenWidth < 1000;
        
        double pixelSize = isSplash ? 5 : 10; // Larger for Main Window
        double spacing = isSplash ? 2 : 4;
        double digitGap = isSplash ? 10 : 20;
        // Calculate Total Width used by "2026"
        double singleDigitWidth = (3 * pixelSize) + (2 * spacing);
        double totalWidth = (4 * singleDigitWidth) + (3 * digitGap);
        double startX = (screenWidth - totalWidth) / 2;
        // On Splash (isSplash), move it down to ~ middle-ish layer or keep top but background
        // User screenshot showed it crowding top. Let's move it down a bit on Splash.
        double startY = isSplash ? 160 : (screenHeight * 0.2); 
        // Set color style
        var brush = isSplash ? new SolidColorBrush(Color.FromRgb(66, 255, 66)) : Brushes.Gold; // Terminal Green vs Gold
        DrawDigit(digit2, startX, startY, pixelSize, spacing, brush);
        startX += singleDigitWidth + digitGap;
        
        DrawDigit(digit0, startX, startY, pixelSize, spacing, brush);
        startX += singleDigitWidth + digitGap;
        
        DrawDigit(digit2, startX, startY, pixelSize, spacing, brush);
        startX += singleDigitWidth + digitGap;
        
        DrawDigit(digit6, startX, startY, pixelSize, spacing, brush);
    }
    private void DrawDigit(string[] mask, double x, double y, double size, double spacing, Brush brush)
    {
        for (int r = 0; r < mask.Length; r++)
        {
            for (int c = 0; c < mask[r].Length; c++)
            {
                if (mask[r][c] == '1')
                {
                    var rect = new Rectangle
                    {
                        Width = size,
                        Height = size,
                        Fill = brush,
                        Opacity = 0.3, // Base opacity (faint background)
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(rect, x + c * (size + spacing));
                    Canvas.SetTop(rect, y + r * (size + spacing));
                    _canvas.Children.Add(rect); 
                    _textPixels.Add(rect);
                }
            }
        }
    }
    private void UpdateTextGlimmer()
    {
        // Sine wave opacity 0.3 to 0.8
        _glimmerPhase += 0.05;
        double opacity = 0.55 + 0.25 * Math.Sin(_glimmerPhase);
        
        foreach (var rect in _textPixels)
        {
            // Add slight randomness per pixel for "glimmer"
            double noise = (_rnd.NextDouble() - 0.5) * 0.1;
            rect.Opacity = Math.Clamp(opacity + noise, 0.2, 1.0);
        }
    }
}
return new HolidayTheme();
