namespace Fluently;

internal sealed partial class DialogService
{
    [LoggerMessage(LogLevel.Error, "{ViewModelType} needs a parameter; show it with a parameter.")]
    private partial void LogParameterRequired(Type viewModelType);

    [LoggerMessage(LogLevel.Error,
        "{ViewModelType} does not implement INavigatedTo<{ParameterType}>, so it cannot be shown with that parameter.")]
    private partial void LogParameterNotAccepted(Type viewModelType, Type parameterType);

    [LoggerMessage(LogLevel.Warning,
        "{ViewModelType} is already open, so it is not shown again; register it as transient to show it twice.")]
    private partial void LogAlreadyOpen(Type viewModelType);

    [LoggerMessage(LogLevel.Error, "Opening the dialog {ViewModelType} failed.")]
    private partial void LogOpenFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Asking the dialog {ViewModelType} whether it can be dismissed failed.")]
    private partial void LogDismissGuardFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Closing the dialog {ViewModelType} failed.")]
    private partial void LogCloseFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Disposing the dialog {ViewModelType} after it closed failed.")]
    private partial void LogDisposeFailed(Type viewModelType, Exception exception);
}
