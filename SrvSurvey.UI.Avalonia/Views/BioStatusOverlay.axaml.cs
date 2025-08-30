using Avalonia.Controls;
using Avalonia;
using SrvSurvey.Core;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class BioStatusOverlay : Window
{
    private TextBlock? _txtBody;
    private TextBlock? _txtSignals;
    private TextBlock? _txtTemperature;
    private TextBlock? _txtLastScan;

    public BioStatusOverlay()
    {
        InitializeComponent();
        _txtBody = this.FindControl<TextBlock>("TxtBody");
        _txtSignals = this.FindControl<TextBlock>("TxtSignals");
        _txtTemperature = this.FindControl<TextBlock>("TxtTemperature");
        _txtLastScan = this.FindControl<TextBlock>("TxtLastScan");
        
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        IsHitTestVisible = false;
        
        // Position bottom-left of primary screen
        var screen = Screens.Primary;
        if (screen != null)
        {
            var b = screen.WorkingArea;
            Position = new PixelPoint((int)(b.X + 20), (int)(b.Y + b.Height - Height - 20));
        }
    }

    public void UpdateBioStatus(string bodyName, int signalCount, double? temperature, string? lastScan = null)
    {
        if (_txtBody != null)
            _txtBody.Text = bodyName;
            
        if (_txtSignals != null)
            _txtSignals.Text = $"Signals: {signalCount}";
            
        if (_txtTemperature != null)
        {
            if (temperature.HasValue)
                _txtTemperature.Text = $"Temperature: {temperature:F1}K";
            else
                _txtTemperature.Text = "Temperature: Unknown";
        }
        
        if (_txtLastScan != null && !string.IsNullOrEmpty(lastScan))
            _txtLastScan.Text = lastScan;
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
