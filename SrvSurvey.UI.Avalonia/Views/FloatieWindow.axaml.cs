using Avalonia.Controls;
using Avalonia;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class FloatieWindow : Window
{
    private TextBlock? _txt;

    public FloatieWindow()
    {
        InitializeComponent();
        _txt = this.FindControl<TextBlock>("Txt");
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        IsHitTestVisible = false; // ignore mouse, like overlay
    }

    public void ShowMessage(string message)
    {
        _txt!.Text = message;
        if (!IsVisible)
            Show();

        // position bottom center of primary screen
        var screen = Screens.Primary;
        if (screen != null)
        {
            var b = screen.WorkingArea;
            Position = new PixelPoint((int)(b.X + (b.Width - Width) / 2), (int)(b.Y + b.Height - Height - 40));
        }
    }
}
