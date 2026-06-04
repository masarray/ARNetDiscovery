namespace ARNetDiscovery.Core.Diagnostics;

public sealed record DiagnosticEntry(
    DateTimeOffset Timestamp,
    DiagnosticSeverity Severity,
    string Source,
    string Message,
    string? ExceptionType = null,
    string? Details = null)
{
    public string DisplayText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ExceptionType) && string.IsNullOrWhiteSpace(Details))
                return Message;

            var detail = string.IsNullOrWhiteSpace(Details) ? string.Empty : Details.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(detail))
                return $"{Message} ({ExceptionType})";

            return $"{Message} ({ExceptionType}: {detail})";
        }
    }

    public static DiagnosticEntry Info(string source, string message) =>
        new(DateTimeOffset.Now, DiagnosticSeverity.Info, source, message);

    public static DiagnosticEntry Warning(string source, string message, Exception? exception = null) =>
        new(DateTimeOffset.Now, DiagnosticSeverity.Warning, source, message, exception?.GetType().Name, exception?.Message);

    public static DiagnosticEntry Error(string source, string message, Exception? exception = null) =>
        new(DateTimeOffset.Now, DiagnosticSeverity.Error, source, message, exception?.GetType().Name, exception?.ToString());
}
