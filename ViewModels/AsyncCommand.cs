using System.Windows.Input;

namespace ChessCore.Mobile.ViewModels;

// ICommand's entry point is async void; the public Task entry point is awaitable.
public sealed class AsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Action<Exception> _onError;
    private readonly Func<bool>? _canExecute;
    private int _running;

    public AsyncCommand(Func<Task> execute, Action<Exception> onError, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _onError = onError;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _running == 0 && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter) => await ExecuteAsync();

    public async Task ExecuteAsync()
    {
        if (!CanExecute(null) || Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;
        NotifyCanExecuteChanged();
        try { await _execute(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _onError(ex); }
        finally { Interlocked.Exchange(ref _running, 0); NotifyCanExecuteChanged(); }
    }

    public void NotifyCanExecuteChanged() => MainThread.BeginInvokeOnMainThread(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
}

public sealed class AsyncCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Action<Exception> _onError;
    private readonly Func<T?, bool>? _canExecute;
    private int _running;

    public AsyncCommand(Func<T?, Task> execute, Action<Exception> onError, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _onError = onError;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_running != 0) return false;
        if (_canExecute == null) return true;
        if (parameter is T typed) return _canExecute(typed);
        return _canExecute(default);
    }

    public async void Execute(object? parameter)
    {
        T? val = default;
        if (parameter is T typed)
        {
            val = typed;
        }
        else if (parameter != null && typeof(T) == typeof(int) && int.TryParse(parameter.ToString(), out var i))
        {
            val = (T)(object)i;
        }
        await ExecuteAsync(val);
    }

    public async Task ExecuteAsync(T? parameter)
    {
        if (!CanExecute(parameter) || Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;
        NotifyCanExecuteChanged();
        try { await _execute(parameter); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _onError(ex); }
        finally { Interlocked.Exchange(ref _running, 0); NotifyCanExecuteChanged(); }
    }

    public void NotifyCanExecuteChanged() => MainThread.BeginInvokeOnMainThread(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
}
