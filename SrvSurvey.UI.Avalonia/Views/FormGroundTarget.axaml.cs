using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class FormGroundTarget : Window
{
    public LatLong2 Target { get; private set; } = LatLong2.Empty;
    public bool TargetSet { get; private set; } = false;

    public FormGroundTarget()
    {
        InitializeComponent();
    }

    private void BtnBegin_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var lat = double.Parse(TxtLat.Text);
            var lon = double.Parse(TxtLong.Text);

            Target = new LatLong2(lat, lon);
            TargetSet = true;

            Close();
        }
        catch (Exception ex)
        {
            // TODO: Show error message
            Console.WriteLine($"Parse error: {ex.Message}");
        }
    }

    private void BtnTargetCurrent_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Get current player position
        // For now, set some default coordinates
        TxtLat.Text = "0.0";
        TxtLong.Text = "0.0";
    }

    private void BtnPaste_Click(object? sender, RoutedEventArgs e)
    {
        var pastedCoords = PasteFromClipboard();
        if (pastedCoords.HasValue)
        {
            TxtLat.Text = pastedCoords.Value.Lat.ToString();
            TxtLong.Text = pastedCoords.Value.Long.ToString();
        }
    }

    private void BtnClear_Click(object? sender, RoutedEventArgs e)
    {
        Target = LatLong2.Empty;
        TargetSet = false;
        Close();
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        TargetSet = false;
        Close();
    }

    public static LatLong2? PasteFromClipboard()
    {
        // TODO: Implement clipboard functionality
        // For now, return null
        return null;
    }
}

public struct LatLong2
{
    public double Lat { get; }
    public double Long { get; }

    public static LatLong2 Empty => new LatLong2(0, 0);

    public LatLong2(double lat, double lon)
    {
        Lat = lat;
        Long = lon;
    }

    public override string ToString()
    {
        return $"{Lat:F6}, {Long:F6}";
    }
}
