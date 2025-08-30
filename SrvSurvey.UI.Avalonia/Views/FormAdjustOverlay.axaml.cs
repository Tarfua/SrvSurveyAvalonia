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

        // For now, add our overlay types
        // TODO: Integrate with actual plotter system
        var overlayNames = new[]
        {
            "System Status",
            "Bio Status",
            "Floatie",
            "Colony Commodities"
        };

        foreach (var name in overlayNames)
        {
            ComboPlotter.Items.Add(name);
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

        // TODO: Load current position settings for this overlay
        // For now, just reset to defaults
        TxtX.Text = "0";
        TxtY.Text = "0";
        CheckCenter.IsChecked = true;
        CheckMiddle.IsChecked = true;
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

    private void BtnAccept_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Save position settings
        // For now, just close with OK result
        Close();
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        TargetName = null;
        // TODO: Restore overlay positions if cancelled
        base.OnClosed(e);
    }
}
