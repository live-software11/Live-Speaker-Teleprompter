using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace TeleprompterApp;

public partial class App : System.Windows.Application
{
	private readonly string _logFilePath;

	public App()
	{
		_logFilePath = Path.Combine(AppPaths.LogDirectory, $"error-{DateTime.Now:yyyyMMdd-HHmmss}.log");
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		// Force hardware GPU rendering for best performance on external screens
		RenderOptions.ProcessRenderMode = RenderMode.Default;

		// Localization: use preferences only; user switches via toolbar selector
		var prefs = PreferencesService.Load();
		Localization.Initialize(null, prefs.CultureName);

		// Clean up old log files (keep last 10)
		CleanupOldLogs();

		base.OnStartup(e);
	}

	private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
	{
		LogException("Dispatcher", e.Exception);
		try
		{
			System.Windows.MessageBox.Show(
				Localization.Get("Error_Unhandled", e.Exception.Message),
				Localization.Get("Error_Title"),
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}
		catch { /* non-fatal */ }

		// SACRED RULE #2: stabilità live. MAI Shutdown in un handler globale:
		// durante un evento live l'app deve restare viva anche dopo un'eccezione.
		// L'operatore chiuderà manualmente a fine show se necessario.
		e.Handled = true;
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
			Directory.CreateDirectory(AppPaths.LogDirectory);
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

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		if (e.Exception?.InnerException != null)
		{
			LogException("TaskScheduler", e.Exception.InnerException);
		}
		e.SetObserved();
	}

	private static void CleanupOldLogs()
	{
		try
		{
			if (!Directory.Exists(AppPaths.LogDirectory)) return;

			var logFiles = new DirectoryInfo(AppPaths.LogDirectory)
				.GetFiles("error-*.log")
				.OrderByDescending(f => f.CreationTimeUtc)
				.Skip(10);

			foreach (var file in logFiles)
			{
				try { file.Delete(); } catch { }
			}
		}
		catch
		{
			// Non-critical, ignore
		}
	}
}
