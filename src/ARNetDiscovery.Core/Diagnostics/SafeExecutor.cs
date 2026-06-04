namespace ARNetDiscovery.Core.Diagnostics;

public static class SafeExecutor
{
    public static async Task RunAsync(string source, Func<Task> action, IDiagnosticSink? diagnostics = null)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            diagnostics?.Publish(DiagnosticEntry.Info(source, "Operation cancelled."));
        }
        catch (Exception ex)
        {
            diagnostics?.Publish(DiagnosticEntry.Error(source, "Handled exception routed to diagnostics.", ex));
        }
    }

    public static async Task<T?> RunAsync<T>(string source, Func<Task<T>> action, IDiagnosticSink? diagnostics = null, T? fallback = default)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            diagnostics?.Publish(DiagnosticEntry.Info(source, "Operation cancelled."));
            return fallback;
        }
        catch (Exception ex)
        {
            diagnostics?.Publish(DiagnosticEntry.Error(source, "Handled exception routed to diagnostics.", ex));
            return fallback;
        }
    }
}
