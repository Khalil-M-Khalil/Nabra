using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Nabra.Interop;
using Nabra.Localization;
using Nabra.Services;

namespace Nabra.Views
{
    public partial class MainWindow : Window
    {
        private readonly AppController _app;
        private readonly Dictionary<string, FrameworkElement> _pages = new Dictionary<string, FrameworkElement>();
        private readonly ShortcutsPage _shortcuts;
        private string _current = string.Empty;
        private bool _loading;

        private double _currentSkewX;
        private double _currentSkewY;
        private readonly System.Windows.Threading.DispatcherTimer _tiltTimer;
        private Point _lastLocation;
        private DateTime _lastTime;

        internal MainWindow(AppController app)
        {
            _app = app;
            InitializeComponent();
            
            ScaleHost.RenderTransformOrigin = new Point(0.5, 0.5);
            
            _tiltTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _tiltTimer.Tick += OnTiltTimerTick;
            this.LocationChanged += MainWindow_LocationChanged;

            _shortcuts = new ShortcutsPage(app);
            _pages["Home"] = new HomePage(app);
            _pages["Shortcuts"] = _shortcuts;
            _pages["Settings"] = new SettingsPage(app);
            _pages["About"] = new AboutPage();

            _app.SettingsApplied += OnSettingsApplied;
            OnSettingsApplied();
            NavigateTo("Home");
        }
        
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Reset state
            _lastTime = default;
            DragMove();
        }
        
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void MinBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var currentLoc = new Point(this.Left, this.Top);
            
            if (_lastTime != default)
            {
                double dt = (now - _lastTime).TotalSeconds;
                if (dt > 0)
                {
                    double vx = (currentLoc.X - _lastLocation.X) / dt;
                    double vy = (currentLoc.Y - _lastLocation.Y) / dt;
                    
                    // The window is being dragged. Map velocity to a skew (tilt).
                    _currentSkewX = Math.Clamp(-vx * 0.003, -8, 8);
                    _currentSkewY = Math.Clamp(-vy * 0.003, -8, 8);
                    
                    var tg = (TransformGroup)ScaleHost.RenderTransform;
                    var skew = (SkewTransform)tg.Children[0];
                    skew.AngleX = _currentSkewX;
                    skew.AngleY = _currentSkewY;
                    
                    _tiltTimer.Start();
                }
            }
            _lastLocation = currentLoc;
            _lastTime = now;
        }

        private void OnTiltTimerTick(object? sender, EventArgs e)
        {
            // Spring physics to return skew to 0
            _currentSkewX += (0 - _currentSkewX) * 0.2;
            _currentSkewY += (0 - _currentSkewY) * 0.2;
            
            var tg = (TransformGroup)ScaleHost.RenderTransform;
            var skew = (SkewTransform)tg.Children[0];
            skew.AngleX = _currentSkewX;
            skew.AngleY = _currentSkewY;
            
            if (Math.Abs(_currentSkewX) < 0.1 && Math.Abs(_currentSkewY) < 0.1)
            {
                skew.AngleX = 0;
                skew.AngleY = 0;
                _tiltTimer.Stop();
            }
        }

        public void NavigateTo(string page)
        {
            if (!_pages.ContainsKey(page)) page = "Home";

            RadioButton target;
            switch (page)
            {
                case "Shortcuts": target = NavShortcuts; break;
                case "Settings": target = NavSettings; break;
                case "About": target = NavAbout; break;
                default: target = NavHome; break;
            }

            if (target.IsChecked == true) ShowPage(page);
            else target.IsChecked = true; // raises Nav_Checked, which shows the page
        }

        public void BeginHotkeyChange()
        {
            NavigateTo("Shortcuts");
            _shortcuts.BeginChange(HotkeyAction.Convert);
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            string key;
            if (ReferenceEquals(sender, NavShortcuts)) key = "Shortcuts";
            else if (ReferenceEquals(sender, NavSettings)) key = "Settings";
            else if (ReferenceEquals(sender, NavAbout)) key = "About";
            else key = "Home";
            ShowPage(key);
        }

        private void ShowPage(string key)
        {
            if (_current == key) return;
            _current = key;

            var page = _pages[key];
            PageHost.Content = page;
            page.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170)));
        }

        private void StatusToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _app.SetEnabled(StatusToggle.IsChecked == true);
        }

        private void OnSettingsApplied()
        {
            var s = _app.Settings.Current;

            _loading = true;
            StatusToggle.IsChecked = s.IsEnabled;
            _loading = false;

            StatusText.Text = Loc.T(s.IsEnabled ? "status_on" : "status_off");
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, s.IsEnabled ? "SuccessBrush" : "DangerBrush");

            UiScaleTransform.ScaleX = s.UiScale;
            UiScaleTransform.ScaleY = s.UiScale;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var app = (App)Application.Current;
            if (!app.IsExiting)
            {
                if (_app.Settings.Current.MinimizeToTrayOnClose)
                {
                    e.Cancel = true;
                    Hide();
                    _app.NotifyHiddenToTrayOnce();
                }
                else
                {
                    _app.RequestExit();
                }
            }
            base.OnClosing(e);
        }
    }
}
