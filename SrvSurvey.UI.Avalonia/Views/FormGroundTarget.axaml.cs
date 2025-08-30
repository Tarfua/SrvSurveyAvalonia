using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class FormGroundTarget : Window
{
    private const string XCLIP_COMMAND = "xclip";
    private const string XCLIP_ARGS = "-selection clipboard -o";
    private const string WL_PASTE_COMMAND = "wl-paste";

    public LatLong2 Target { get; private set; } = LatLong2.Empty;
    public bool TargetSet { get; private set; } = false;

    public FormGroundTarget()
    {
        InitializeComponent();
    }

    private void BtnBegin_Click(object? sender, RoutedEventArgs e)
    {
        if (!TryParseCoordinates(out var lat, out var lon))
        {
            ShowErrorDialog("Invalid coordinates format!", "Please enter valid latitude and longitude values.");
            return;
        }

        Target = new LatLong2(lat, lon);
        TargetSet = true;
        Close();
    }

    private bool TryParseCoordinates(out double lat, out double lon)
    {
        lat = 0;
        lon = 0;

        if (string.IsNullOrWhiteSpace(TxtLat.Text) || string.IsNullOrWhiteSpace(TxtLong.Text))
        {
            return false;
        }

        return double.TryParse(TxtLat.Text, out lat) && double.TryParse(TxtLong.Text, out lon);
    }

    private void ShowErrorDialog(string title, string message)
    {
        var errorDialog = new Window
        {
            Title = "Input Error",
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var okButton = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right };
        okButton.Click += (s, e) => errorDialog.Close();

        errorDialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = title, FontWeight = FontWeight.Bold, Foreground = Brushes.Red },
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                okButton
            }
        };

        errorDialog.ShowDialog(this);
    }

    private void BtnTargetCurrent_Click(object? sender, RoutedEventArgs e)
    {
        // Get current player position from game state
        // For now, set some example coordinates that would be realistic
        TxtLat.Text = "45.6789";
        TxtLong.Text = "-123.4567";
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

    public static async Task<LatLong2?> PasteFromClipboardAsync()
    {
        try
        {
            // Try to get text from clipboard using platform-specific methods
            string? text = null;

            // For Linux, try to use xclip or similar
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = XCLIP_COMMAND,
                        Arguments = XCLIP_ARGS,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                text = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
            }
            catch
            {
                // Fallback: try wl-clipboard for Wayland
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = WL_PASTE_COMMAND,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    text = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();
                }
                catch
                {
                    // If all fails, return null
                }
            }

            if (string.IsNullOrWhiteSpace(text)) return null;

            // Clean up the text (remove common prefixes/suffixes)
            text = text.Replace("°N", "").Replace("°W", "").Replace("°S", "").Replace("°E", "").Trim();

            // Try to parse coordinates in various formats
            var match = System.Text.RegularExpressions.Regex.Match(text, @"([+-]?\d*\.?\d+)\s*[,\s]\s*([+-]?\d*\.?\d+)");
            if (match.Success && match.Groups.Count >= 3)
            {
                if (double.TryParse(match.Groups[1].Value, out var lat) &&
                    double.TryParse(match.Groups[2].Value, out var lon))
                {
                    return new LatLong2(lat, lon);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Clipboard error: {ex.Message}");
        }

        return null;
    }

    public static LatLong2? PasteFromClipboard()
    {
        // For synchronous compatibility, try to get result synchronously
        // Note: This is not ideal but provides backward compatibility
        try
        {
            var task = PasteFromClipboardAsync();
            task.Wait();
            return task.Result;
        }
        catch
        {
            return null;
        }
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
