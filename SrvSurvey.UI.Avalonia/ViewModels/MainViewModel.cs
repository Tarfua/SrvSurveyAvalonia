using ReactiveUI;
using System.Reactive;
using SrvSurvey.Core;

namespace SrvSurvey.UI.Avalonia.ViewModels;

public sealed class MainViewModel : ReactiveObject
{
    private string _logs = string.Empty;
    public string Logs
    {
        get => _logs;
        set => this.RaiseAndSetIfChanged(ref _logs, value);
    }

    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public MainViewModel()
    {
        ExitCommand = ReactiveCommand.Create(() => { });
        Logging.Message += line => Logs += line + "\n";
    }
}


