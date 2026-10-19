namespace Fluently;

internal sealed partial class NavigationService
{
    [LoggerMessage(LogLevel.Error,
        "{ViewModelType} needs a navigation parameter; navigate with NavigateToAsync<TPage, TParameter>.")]
    private partial void LogParameterRequired(Type viewModelType);

    [LoggerMessage(LogLevel.Error,
        "{ViewModelType} does not implement INavigatedTo<{ParameterType}>, so it cannot be navigated to with that parameter.")]
    private partial void LogParameterNotAccepted(Type viewModelType, Type parameterType);

    [LoggerMessage(LogLevel.Error, "Navigation to {ViewModelType} failed.")]
    private partial void LogNavigationFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Asking {ViewModelType} whether it can be left failed.")]
    private partial void LogNavigationGuardFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Leaving {ViewModelType} after failed navigation failed.")]
    private partial void LogNavigationCleanupFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Restoring {ViewModelType} after failed navigation failed.")]
    private partial void LogNavigationRestoreFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Disposing {ViewModelType} after it left the history failed.")]
    private partial void LogPageDisposeFailed(Type viewModelType, Exception exception);
}
