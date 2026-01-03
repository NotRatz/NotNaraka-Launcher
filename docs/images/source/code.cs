using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NotNarakaLauncher.App.Interfaces;

namespace NotNarakaLauncher.App
{
    public class HolidayTheme : IThemeEffect
    {
        private Canvas _canvas;
        private Random _rnd = new();
        private bool _isRunning = false;
        private EventHandler _renderHandler;
        private Stopwatch _stopwatch = new();
        private double _lastTime = 0;
        
        // Particles
        private List<FireworkParticle> _particles = new();
        private double _timeSinceLastLaunch = 0;
        
        // 2026 Logic
        private List<Point> _textTargets = new();
        private double _specialTimer = 0;
        private double _nextSpecialInterval = 15; // Seconds
        
        // Configuration
        private const double TargetFps = 60.0;
        private const double BaseGravity = 0.05;
        private const double SpeedScale = 0.7; // Global slow-down factor (0.7 = 70% speed)
        
        private class FireworkParticle
        {
            public Ellipse Element;
            public double X, Y;
            public double VX, VY;
            public double Alpha;
            public Color Color;
            
            // State
            public ParticleMode Mode;
            public Point? Target;
            public double HoldTime; // How long to stay in text
        }
        
        private enum ParticleMode
        {
            Rocket,
            Spark,
            seek,
            Hold,
            Fade
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
            _textTargets.Clear();
            _specialTimer = 0;
            _nextSpecialInterval = _rnd.Next(15, 30); // More frequent for testing, or user pref
            
            _stopwatch.Restart();
            _lastTime = _stopwatch.Elapsed.TotalSeconds;

            CompositionTarget.Rendering -= _renderHandler;
            CompositionTarget.Rendering += _renderHandler;

            // Pre-calculate text targets if canvas has size
            if (_canvas.ActualWidth > 0) CalculateTextTargets();
        }

        public void Stop()
        {
            _isRunning = false;
            CompositionTarget.Rendering -= _renderHandler;
            _stopwatch.Stop();
            
            if (_canvas != null)
            {
                _canvas.Children.Clear();
                _canvas = null;
            }
            _particles.Clear();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_isRunning || _canvas == null) return;
            
            try
            {
                // Pause logic (basic visibility check)
                if (_canvas.PresentationSource == null) return;

                double currentTime = _stopwatch.Elapsed.TotalSeconds;
                double dt = currentTime - _lastTime;
                _lastTime = currentTime;

                // Cap DeltaTime to prevent explosion after lag spike
                if (dt > 0.1) dt = 0.1;

                // Normalize to TargetFps (so 1.0 = 1 frame at 60fps)
                // This allows us to keep using similar magic numbers as before by scaling them
                double timeFactor = dt * TargetFps * SpeedScale; 

                // Recalculate targets if size changed (dramatically) or not set
                if (_textTargets.Count == 0 && _canvas.ActualWidth > 0) CalculateTextTargets();

                UpdateLogic(timeFactor, dt);
            }
            catch (Exception)
            {
                // Fail silently to prevent app crash/freeze
                // In debug we might want to log this
            }
        }

        private void UpdateLogic(double tf, double dtSeconds)
        {
            // 1. Normal Fireworks Launch
            _timeSinceLastLaunch += dtSeconds;
            // Launch slightly less frequently if we are slowing down time
            if (_timeSinceLastLaunch > (0.8 / SpeedScale) && _rnd.NextDouble() < (0.1 * tf)) 
            {
                LaunchNormalRocket();
                _timeSinceLastLaunch = 0;
            }

            // 2. Special 2026 Launch
            _specialTimer += dtSeconds;
            if (_specialTimer >= _nextSpecialInterval)
            {
                LaunchSpecialRocket();
                _specialTimer = 0;
                _nextSpecialInterval = _rnd.Next(20, 60); // Reset interval
            }

            // 3. Update Particles
            double height = _canvas.ActualHeight;

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];

                switch (p.Mode)
                {
                    case ParticleMode.Rocket:
                        p.X += p.VX * tf;
                        p.Y += p.VY * tf;
                        p.VY += BaseGravity * tf; // Gravity
                        
                        // Explode logic
                        if (p.Target != null) // Special Rocket has a "target" area (center)
                        {
                            // If reached peak or target Y roughly
                             if (p.VY >= -0.5) 
                             { 
                                 SafeExplodeSpecial(p); 
                                 RemoveParticle(i); 
                                 continue; 
                             }
                        }
                        else
                        {
                             if (p.VY >= -0.5 || p.Y < height * 0.1) 
                             { 
                                 ExplodeNormal(p); 
                                 RemoveParticle(i); 
                                 continue; 
                             }
                        }
                        break;

                    case ParticleMode.Spark:
                        p.X += p.VX * tf;
                        p.Y += p.VY * tf;
                        p.VY += (BaseGravity * 1.5) * tf; // Sprax slightly heavier
                        p.Alpha -= 0.015 * tf;
                        if (p.Alpha <= 0) 
                        { 
                            RemoveParticle(i); 
                            continue; 
                        }
                        p.Element.Opacity = p.Alpha;
                        break;

                    case ParticleMode.seek:
                        // Seek Target
                        if (p.Target.HasValue)
                        {
                            double dx = p.Target.Value.X - p.X;
                            double dy = p.Target.Value.Y - p.Y;
                            
                            // Framerate independent easing
                            // Lerp: New = Current + (Target - Current) * factor
                            // Factor needs to be time based. 
                            double seekSpeed = 0.1 * tf; 
                            if (seekSpeed > 0.9) seekSpeed = 0.9; // Clamp

                            p.X += dx * seekSpeed;
                            p.Y += dy * seekSpeed;
                            
                            if (Math.Abs(dx) < 2 && Math.Abs(dy) < 2)
                            {
                                p.X = p.Target.Value.X;
                                p.Y = p.Target.Value.Y;
                                p.Mode = ParticleMode.Hold;
                                p.HoldTime = 5.0; // Hold for 5 seconds
                                p.Color = Colors.Gold; // Turn Gold
                                ((SolidColorBrush)p.Element.Fill).Color = Colors.Gold;
                            }
                        }
                        else
                        {
                            // Fallback if target lost
                            p.Mode = ParticleMode.Fade;
                        }
                        break;

                    case ParticleMode.Hold:
                        p.HoldTime -= dtSeconds;
                        if (p.HoldTime <= 0) p.Mode = ParticleMode.Fade;
                        // Glimmer High Opacity for Readability
                        if (_rnd.NextDouble() < (0.2 * tf)) 
                            p.Element.Opacity = 0.8 + _rnd.NextDouble() * 0.2;
                        else 
                            p.Element.Opacity = 1.0;
                        break;

                    case ParticleMode.Fade:
                        p.Y += 0.5 * tf; // Fall slowly
                        p.Alpha -= 0.01 * tf;
                        if (p.Alpha <= 0) 
                        { 
                            RemoveParticle(i); 
                            continue; 
                        }
                        p.Element.Opacity = p.Alpha;
                        break;
                }

                // Bounds check removal (optimization)
                if (p.Y > height + 50)
                {
                    RemoveParticle(i);
                    continue;
                }

                // Render
                Canvas.SetLeft(p.Element, p.X);
                Canvas.SetTop(p.Element, p.Y);
            }
        }

        private void LaunchNormalRocket()
        {
            double startX = _rnd.NextDouble() * _canvas.ActualWidth * 0.8 + (_canvas.ActualWidth * 0.1);
            LaunchRocket(startX, null);
        }

        private void LaunchSpecialRocket()
        {
            LaunchRocket(_canvas.ActualWidth / 2, new Point(_canvas.ActualWidth / 2, _canvas.ActualHeight * 0.3));
        }

        private void LaunchRocket(double startX, Point? target)
        {
            double startY = _canvas.ActualHeight + 10;
            bool isSplash = _canvas.ActualWidth < 1000;
            var color = GetRandomColor(isSplash);
            
            var p = new FireworkParticle
            {
                X = startX,
                Y = startY,
                VX = (_rnd.NextDouble() - 0.5) * 1.5, 
                VY = -(_rnd.NextDouble() * 4.0 + 11.0), // Initial upward velocity
                Mode = ParticleMode.Rocket,
                Color = color,
                Alpha = 1.0,
                Target = target,
                Element = CreateEllipse(4, color) // Slightly larger rocket
            };
            _particles.Add(p);
            _canvas.Children.Add(p.Element);
        }

        private void ExplodeNormal(FireworkParticle rocket)
        {
            int count = _rnd.Next(20, 40); // Fewer particles for performance
            for (int i = 0; i < count; i++) SpawnSpark(rocket, false);
        }

        private void SafeExplodeSpecial(FireworkParticle rocket)
        {
            try
            {
                // Calculate Text Targets again to be sure of size
                if (_textTargets.Count == 0) CalculateTextTargets();
                
                // Spawn 1 particle per target + some extras for flair
                // LIMIT the number to prevent freeze if something goes wrong
                int maxSpawns = 250; 
                int spawned = 0;

                foreach (var target in _textTargets)
                {
                     if (spawned++ > maxSpawns) break;
                     SpawnSpark(rocket, true, target);
                }
                
                // Extra sparks that fall normally
                for(int i=0; i<30; i++) SpawnSpark(rocket, false);
            }
            catch {}
        }

        private void SpawnSpark(FireworkParticle parent, bool isSeeking, Point? target = null)
        {
            double angle = _rnd.NextDouble() * Math.PI * 2;
            double speed = _rnd.NextDouble() * 3.0 + 1.0;
            
            var p = new FireworkParticle
            {
                X = parent.X,
                Y = parent.Y,
                VX = Math.Cos(angle) * speed,
                VY = Math.Sin(angle) * speed,
                Mode = isSeeking ? ParticleMode.seek : ParticleMode.Spark,
                Target = target,
                Color = parent.Color,
                Alpha = 1.0,
                Element = CreateEllipse(isSeeking ? 4 : 2, parent.Color)
            };
            _particles.Add(p);
            _canvas.Children.Add(p.Element);
        }

        private void RemoveParticle(int index)
        {
            if (index < 0 || index >= _particles.Count) return;
            try { _canvas.Children.Remove(_particles[index].Element); } catch { }
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
            if (e.Fill.CanFreeze) e.Fill.Freeze();
            return e;
        }

        private Color GetRandomColor(bool isSplash)
        {
            // Splash: Mostly Green/White. Main: Festive.
            if (isSplash) return _rnd.NextDouble() > 0.7 ? Colors.White : Color.FromRgb(66, 255, 66);
            var colors = new[] { 
                Colors.Cyan, Colors.Magenta, Colors.Yellow, Colors.Lime, 
                Color.FromRgb(255, 80, 80), Colors.Gold 
            };
            return colors[_rnd.Next(colors.Length)];
        }

        private void CalculateTextTargets()
        {
            _textTargets.Clear();
            string[] digit2 = { "111", "001", "111", "100", "111" };
            string[] digit0 = { "111", "101", "101", "101", "111" };
            string[] digit6 = { "111", "100", "111", "101", "111" };

            double screenWidth = _canvas.ActualWidth;
            
            // Adjust logic for new Main Window Width (1583)
            bool isSplash = screenWidth < 1000;
            
            double pixelSize = isSplash ? 5 : 8; 
            double spacing = isSplash ? 2 : 3;
            double digitGap = isSplash ? 12 : 24;

            double singleDigitWidth = (3 * pixelSize) + (2 * spacing);
            double totalWidth = (4 * singleDigitWidth) + (3 * digitGap);

            double startX = (screenWidth - totalWidth) / 2;
            double startY = isSplash ? 160 : 80;

            AddDigitTargets(digit2, startX, startY, pixelSize, spacing);
            startX += singleDigitWidth + digitGap;
            AddDigitTargets(digit0, startX, startY, pixelSize, spacing);
            startX += singleDigitWidth + digitGap;
            AddDigitTargets(digit2, startX, startY, pixelSize, spacing);
            startX += singleDigitWidth + digitGap;
            AddDigitTargets(digit6, startX, startY, pixelSize, spacing);
        }

        private void AddDigitTargets(string[] mask, double x, double y, double size, double spacing)
        {
            for (int r = 0; r < mask.Length; r++)
            {
                for (int c = 0; c < mask[r].Length; c++)
                {
                    if (mask[r][c] == '1')
                    {
                        double px = x + c * (size + spacing) + (size/2);
                        double py = y + r * (size + spacing) + (size/2);
                        _textTargets.Add(new Point(px, py));
                    }
                }
            }
        }
    }
}
