using System;
using System.Threading.Tasks;

namespace MicroluxErgConnect.Infrastructure;

public sealed class RelayCommand
{
    private readonly Func<bool>? _canExecute;
    private readonly Action? _execute;
    private readonly Func<Task>? _executeAsync;
    private bool _isExecuting;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting) return false;
        return _canExecute?.Invoke() ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (_execute != null)
        {
            _execute();
        }
        else if (_executeAsync != null)
        {
            _isExecuting = true;
            try
            {
                RaiseCanExecuteChanged();
                await _executeAsync();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
