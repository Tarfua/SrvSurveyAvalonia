using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Timers;
using System;
using SrvSurvey.Core;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class ColonyCommoditiesOverlay : OverlayWindowBase
{
    private TextBlock? _txtHeader;
    private TextBlock? _txtStatus;
    private TextBlock? _txtPending;
    private StackPanel? _commoditiesPanel;
    private ScrollViewer? _commoditiesScroll;

    public ColonyCommoditiesOverlay()
    {
        InitializeComponent();
        _txtHeader = this.FindControl<TextBlock>("TxtHeader");
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");
        _txtPending = this.FindControl<TextBlock>("TxtPending");
        _commoditiesPanel = this.FindControl<StackPanel>("CommoditiesPanel");
        _commoditiesScroll = this.FindControl<ScrollViewer>("CommoditiesScroll");

        // Configure as overlay window
        SystemDecorations = SystemDecorations.None;
        WindowState = WindowState.Normal;
        WindowStartupLocation = WindowStartupLocation.Manual;

        // Set transparent background
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

        // Position based on saved settings or default
        PositionOnScreen();
    }

    private void PositionOnScreen()
    {
        var settings = AppConfig.Load();
        var overlayName = "Colony Commodities";

        if (settings.OverlayPositions != null && settings.OverlayPositions.TryGetValue(overlayName, out var position))
        {
            // Use saved position
            var screen = Screens.Primary;
            if (screen != null)
            {
                var bounds = screen.WorkingArea;
                int x = (int)(bounds.X + position.X);
                int y = (int)(bounds.Y + position.Y);

                // Ensure window stays within screen bounds
                x = Math.Max(bounds.X, Math.Min(x, bounds.X + bounds.Width - (int)Width));
                y = Math.Max(bounds.Y, Math.Min(y, bounds.Y + bounds.Height - (int)Height));

                Position = new PixelPoint(x, y);
            }
        }
        else
        {
            // Use default position (right side)
            var screen = Screens.Primary;
            if (screen != null)
            {
                var bounds = screen.WorkingArea;
                Position = new PixelPoint((int)(bounds.X + bounds.Width - Width - 20), (int)(bounds.Y + 100));
            }
        }
    }

    public void UpdateCommodities(Dictionary<string, int> commodities, string projectName = "")
    {
        if (_commoditiesPanel == null || _txtStatus == null) return;

        // Clear existing commodities
        _commoditiesPanel.Children.Clear();

        if (commodities.Count == 0)
        {
            _txtStatus.Text = "No commodities needed";
            return;
        }

        _txtStatus.Text = $"Project: {projectName}";
        
        // Add commodity items
        foreach (var kvp in commodities)
        {
            var commodityName = kvp.Key;
            var amount = kvp.Value;
            
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(0, 2)
            };
            
            var nameBlock = new TextBlock
            {
                Text = commodityName,
                FontSize = 10,
                Foreground = Brushes.White,
                Width = 120,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            
            var amountBlock = new TextBlock
            {
                Text = amount.ToString("N0"),
                FontSize = 10,
                Foreground = Brushes.LightGreen,
                FontWeight = FontWeight.Bold,
                Width = 60,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            itemPanel.Children.Add(nameBlock);
            itemPanel.Children.Add(amountBlock);
            
            _commoditiesPanel.Children.Add(itemPanel);
        }
    }

    public void SetPendingStatus(string status)
    {
        if (_txtPending != null)
            _txtPending.Text = status;
    }

    public void ShowOverlay()
    {
        if (!IsVisible)
            Show();
    }

    public void HideOverlay()
    {
        if (IsVisible)
            Hide();
    }
}
