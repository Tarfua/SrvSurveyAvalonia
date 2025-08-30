using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class FormAdjustOverlay : Window
{
    public static string? TargetName { get; set; }
    private bool _changing = false;

    // Controls for enabling/disabling
    private List<Control> _enablementControls = new();

    public FormAdjustOverlay()
    {
        InitializeComponent();

        // Initialize enablement controls
        _enablementControls = new List<Control>
        {
            BtnReset,
            CheckLeft, CheckCenter, CheckRight,
            CheckTop, CheckMiddle, CheckBottom,
            TxtX, TxtY,
            CheckOpacity, TxtOpacity
        };

        PrepPlotters();
        ComboPlotter.SelectedIndex = 0;
        ResetForm();

        // Setup event handlers
        CheckLeft.Checked += CheckHorizontal_CheckedChanged;
        CheckCenter.Checked += CheckHorizontal_CheckedChanged;
        CheckRight.Checked += CheckHorizontal_CheckedChanged;
        CheckTop.Checked += CheckVertical_CheckedChanged;
        CheckMiddle.Checked += CheckVertical_CheckedChanged;
        CheckBottom.Checked += CheckVertical_CheckedChanged;
    }

    private void ResetForm()
    {
        _changing = true;

        // Reset controls
        foreach (var ctrl in _enablementControls)
        {
            ctrl.IsEnabled = false;
        }

        TxtX.Text = "0";
        TxtY.Text = "0";
        CheckCenter.IsChecked = true;
        CheckMiddle.IsChecked = true;

        _changing = false;
    }

    private void PrepPlotters()
    {
        TargetName = null;

        // Clear existing items except first one
        while (ComboPlotter.Items.Count > 1)
        {
            ComboPlotter.Items.RemoveAt(1);
        }

        // Integrate with actual overlay system
        var overlayTypes = new[]
        {
            new { Name = "System Status", Type = "SystemStatus" },
            new { Name = "Bio Status", Type = "BioStatus" },
            new { Name = "Floatie", Type = "Floatie" },
            new { Name = "Colony Commodities", Type = "ColonyCommodities" }
        };

        foreach (var overlay in overlayTypes)
        {
            ComboPlotter.Items.Add(overlay.Name);
        }
    }

    private void ComboPlotter_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ComboPlotter.SelectedIndex <= 0)
        {
            ResetForm();
            return;
        }

        // Enable controls
        foreach (var ctrl in _enablementControls)
        {
            ctrl.IsEnabled = true;
        }

        var selectedName = ComboPlotter.SelectedItem?.ToString();
        TargetName = selectedName;

        // Load current position settings for this overlay
        LoadOverlayPosition(selectedName);
    }

    private void CheckHorizontal_CheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_changing) return;

        _changing = true;

        // Ensure only one horizontal option is checked
        if (sender == CheckLeft)
        {
            CheckCenter.IsChecked = false;
            CheckRight.IsChecked = false;
        }
        else if (sender == CheckCenter)
        {
            CheckLeft.IsChecked = false;
            CheckRight.IsChecked = false;
        }
        else if (sender == CheckRight)
        {
            CheckLeft.IsChecked = false;
            CheckCenter.IsChecked = false;
        }

        _changing = false;
    }

    private void CheckVertical_CheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_changing) return;

        _changing = true;

        // Ensure only one vertical option is checked
        if (sender == CheckTop)
        {
            CheckMiddle.IsChecked = false;
            CheckBottom.IsChecked = false;
        }
        else if (sender == CheckMiddle)
        {
            CheckTop.IsChecked = false;
            CheckBottom.IsChecked = false;
        }
        else if (sender == CheckBottom)
        {
            CheckTop.IsChecked = false;
            CheckMiddle.IsChecked = false;
        }

        _changing = false;
    }

    private void BtnReset_Click(object? sender, RoutedEventArgs e)
    {
        ResetForm();
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        TargetName = null;
        // Restore overlay positions if cancelled
        // TODO: Implement position restoration
        base.OnClosed(e);
    }

    private void LoadOverlayPosition(string? overlayName)
    {
        if (string.IsNullOrEmpty(overlayName))
        {
            ResetToDefaults();
            return;
        }

        // TODO: Load from settings file
        // For now, load from a simple in-memory storage
        var settings = LoadOverlaySettings();
        if (settings.TryGetValue(overlayName, out var position))
        {
            TxtX.Text = position.X.ToString();
            TxtY.Text = position.Y.ToString();
            CheckLeft.IsChecked = position.HorizontalAlign == "Left";
            CheckCenter.IsChecked = position.HorizontalAlign == "Center";
            CheckRight.IsChecked = position.HorizontalAlign == "Right";
            CheckTop.IsChecked = position.VerticalAlign == "Top";
            CheckMiddle.IsChecked = position.VerticalAlign == "Middle";
            CheckBottom.IsChecked = position.VerticalAlign == "Bottom";
        }
        else
        {
            ResetToDefaults();
        }
    }

    private void ResetToDefaults()
    {
        TxtX.Text = "0";
        TxtY.Text = "0";
        CheckCenter.IsChecked = true;
        CheckMiddle.IsChecked = true;
    }

    private Dictionary<string, OverlayPosition> LoadOverlaySettings()
    {
        // TODO: Load from persistent storage
        // For now, return empty dictionary
        return new Dictionary<string, OverlayPosition>();
    }

    private void SaveOverlaySettings(string overlayName, OverlayPosition position)
    {
        // TODO: Save to persistent storage
        // For now, just log the settings
        Console.WriteLine($"Saving position for {overlayName}: X={position.X}, Y={position.Y}, H={position.HorizontalAlign}, V={position.VerticalAlign}");
    }

    private OverlayPosition GetCurrentPosition()
    {
        var position = new OverlayPosition();

        if (double.TryParse(TxtX.Text, out var x)) position.X = x;
        if (double.TryParse(TxtY.Text, out var y)) position.Y = y;

        if (CheckLeft.IsChecked == true) position.HorizontalAlign = "Left";
        else if (CheckCenter.IsChecked == true) position.HorizontalAlign = "Center";
        else if (CheckRight.IsChecked == true) position.HorizontalAlign = "Right";

        if (CheckTop.IsChecked == true) position.VerticalAlign = "Top";
        else if (CheckMiddle.IsChecked == true) position.VerticalAlign = "Middle";
        else if (CheckBottom.IsChecked == true) position.VerticalAlign = "Bottom";

        return position;
    }

    private void BtnAccept_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Save position settings
        if (!string.IsNullOrEmpty(TargetName))
        {
            var position = GetCurrentPosition();
            SaveOverlaySettings(TargetName, position);
        }

        Close();
    }
}

public class OverlayPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public string HorizontalAlign { get; set; } = "Center";
    public string VerticalAlign { get; set; } = "Middle";
}
