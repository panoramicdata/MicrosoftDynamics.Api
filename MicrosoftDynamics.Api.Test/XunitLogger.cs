using System.Globalization;

namespace MicrosoftDynamics.Api.Test;

/// <summary>
/// Logger that writes to xUnit test output
/// </summary>
internal sealed class XunitLogger(ITestOutputHelper output, string categoryName, LogLevel minLevel = LogLevel.Debug) : ILogger
{
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => logLevel >= minLevel;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		ArgumentNullException.ThrowIfNull(formatter);

		if (!IsEnabled(logLevel))
		{
			return;
		}

		var message = formatter(state, exception);
		var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
		var logLevelString = logLevel switch
		{
			LogLevel.Trace => "TRACE",
			LogLevel.Debug => "DEBUG",
			LogLevel.Information => "INFO ",
			LogLevel.Warning => "WARN ",
			LogLevel.Error => "ERROR",
			LogLevel.Critical => "CRIT ",
			_ => "NONE "
		};

		var logOutput = $"[{timestamp}] [{logLevelString}] [{categoryName}] {message}";

		if (eventId.Id != 0)
		{
			logOutput = $"[{timestamp}] [{logLevelString}] [{categoryName}] [EventId:{eventId.Id}] {message}";
		}

		try
		{
			output.WriteLine(logOutput);

			if (exception != null)
			{
				output.WriteLine($"Exception: {exception}");
			}
		}
		catch (InvalidOperationException)
		{
			// If test output is not available, ignore
		}
	}
}
