using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using System.Collections.Generic;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class ColonyCommoditiesOverlay : Window
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
        
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        IsHitTestVisible = false;
        
        // Position right side of primary screen
        var screen = Screens.Primary;
        if (screen != null)
        {
            var b = screen.WorkingArea;
            Position = new PixelPoint((int)(b.X + b.Width - Width - 20), (int)(b.Y + 100));
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
