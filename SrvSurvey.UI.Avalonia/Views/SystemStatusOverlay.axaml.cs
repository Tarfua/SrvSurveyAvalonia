using Avalonia.Controls;
using Avalonia;
using SrvSurvey.Core;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class SystemStatusOverlay : Window
{
    private TextBlock? _txtStatus;
    private TextBlock? _txtHeader;

    public SystemStatusOverlay()
    {
        InitializeComponent();
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");
        _txtHeader = this.FindControl<TextBlock>("TxtHeader");
        
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        IsHitTestVisible = false;
        
        // Position top-right of primary screen
        var screen = Screens.Primary;
        if (screen != null)
        {
            var b = screen.WorkingArea;
            Position = new PixelPoint((int)(b.X + b.Width - Width - 20), (int)(b.Y + 20));
        }
    }

    public void UpdateStatus(string status, string? header = null)
    {
        if (_txtStatus != null)
            _txtStatus.Text = status;
            
        if (_txtHeader != null && header != null)
            _txtHeader.Text = header;
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
