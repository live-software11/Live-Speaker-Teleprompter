using System;
using System.IO;
using System.Windows;
namespace TeleprompterApp;

public partial class App : System.Windows.Application
{
	// Log to %APPDATA% so the portable exe folder stays clean
	private static readonly string LogDirectory = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"R-Speaker Teleprompter", "logs");
	private readonly string _logFilePath;

	public App()
	{
		_logFilePath = Path.Combine(LogDirectory, $"error-{DateTime.Now:yyyyMMdd-HHmmss}.log");
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
	}

	private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
	{
		LogException("Dispatcher", e.Exception);
	System.Windows.MessageBox.Show($"Errore imprevisto: {e.Exception.Message}\nDettagli salvati nei log.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
		e.Handled = true;
		Shutdown(-1);
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception ex)
		{
			LogException("Domain", ex);
		}
	}

	private void LogException(string source, Exception exception)
	{
		try
		{
			Directory.CreateDirectory(LogDirectory);
			using var writer = new StreamWriter(_logFilePath, append: true);
			writer.WriteLine($"[{DateTime.Now:O}] Source: {source}");
			writer.WriteLine(exception);
			writer.WriteLine();
		}
		catch
		{
			// Ignore logging failures.
		}
	}
}
