using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace JiME
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load( this );
		}

		public override void OnFrameworkInitializationCompleted()
		{
			Utils.Init();
			TileGraphics.Init();

			//"App Version" shown in the editor = the build version (set from the git tag by CI)
			string version = typeof( App ).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
			if ( !string.IsNullOrEmpty( version ) )
				Utils.appVersion = version.Split( '+' )[0];

			//UI hooks for the UI-free core
			FileManager.ReportError = ( message, title ) =>
				Dispatcher.UIThread.Post( () => _ = MessageBox.Show( null, message, title, MessageBoxButton.OK, MessageBoxImage.Error ) );
			FileManager.RequestSavePath = ( title, folder, suggestedName ) =>
				FilePickers.SaveJimeFile( ActiveWindow(), title, folder, suggestedName );

			if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
			{
				desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
#if !DEBUG
				//generic, app-wide error handler to catch any unhandled exceptions
				Dispatcher.UIThread.UnhandledException += ( s, e ) =>
				{
					e.Handled = true;
					Exception ex = e.Exception.InnerException ?? e.Exception;
					_ = MessageBox.Show( ActiveWindow(), "An unhandled exception occurred: \r\n\r\n" + ex.Message + "\r\n\r\nStack Trace:\r\n" + ex.StackTrace,
						"App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				};
#endif
				desktop.MainWindow = new ProjectWindow();
			}

			base.OnFrameworkInitializationCompleted();
		}

		/// <summary>
		/// the focused window, or the main window
		/// </summary>
		public static Window ActiveWindow()
		{
			if ( Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
			{
				foreach ( var w in desktop.Windows )
					if ( w.IsActive )
						return w;
				return desktop.MainWindow;
			}
			return null;
		}
	}
}
