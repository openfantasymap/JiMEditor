using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace JiME
{
	public enum MessageBoxButton { OK, OKCancel, YesNoCancel, YesNo }
	public enum MessageBoxImage { None, Error, Question, Warning, Information }
	public enum MessageBoxResult { None, OK, Cancel, Yes, No }

	/// <summary>
	/// Replacement for WPF's MessageBox. Same arguments, but it must be awaited:
	///   if ( await MessageBox.Show( this, "Sure?", "Title", MessageBoxButton.YesNo, MessageBoxImage.Question ) == MessageBoxResult.Yes )
	/// </summary>
	public static class MessageBox
	{
		public static Task<MessageBoxResult> Show( string text, string caption = "", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None )
		{
			return Show( null, text, caption, buttons, image );
		}

		public static async Task<MessageBoxResult> Show( Window owner, string text, string caption = "", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None )
		{
			owner ??= App.ActiveWindow();

			//closing the window with the title bar button gives the "cancel" answer
			MessageBoxResult result = buttons switch
			{
				MessageBoxButton.OK => MessageBoxResult.OK,
				MessageBoxButton.YesNo => MessageBoxResult.No,
				_ => MessageBoxResult.Cancel
			};

			var window = new Window
			{
				Title = string.IsNullOrEmpty( caption ) ? "JiME Editor" : caption,
				SizeToContent = SizeToContent.WidthAndHeight,
				CanResize = false,
				ShowInTaskbar = false,
				MinWidth = 340,
				MaxWidth = 640,
				WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
			};

			var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness( 0, 16, 0, 0 ) };
			void AddButton( string label, MessageBoxResult value, bool isDefault = false, bool isCancel = false )
			{
				var b = new Button { Content = label, MinWidth = 80, IsDefault = isDefault, IsCancel = isCancel, Background = new SolidColorBrush( Color.Parse( "#FF5E5E63" ) ) };
				b.Click += ( s, e ) =>
				{
					result = value;
					window.Close();
				};
				buttonRow.Children.Add( b );
			}

			switch ( buttons )
			{
				case MessageBoxButton.OK:
					AddButton( "OK", MessageBoxResult.OK, true, true );
					break;
				case MessageBoxButton.OKCancel:
					AddButton( "OK", MessageBoxResult.OK, true );
					AddButton( "Cancel", MessageBoxResult.Cancel, false, true );
					break;
				case MessageBoxButton.YesNo:
					AddButton( "Yes", MessageBoxResult.Yes, true );
					AddButton( "No", MessageBoxResult.No, false, true );
					break;
				case MessageBoxButton.YesNoCancel:
					AddButton( "Yes", MessageBoxResult.Yes, true );
					AddButton( "No", MessageBoxResult.No );
					AddButton( "Cancel", MessageBoxResult.Cancel, false, true );
					break;
			}

			var body = new DockPanel { Margin = new Thickness( 20 ) };
			DockPanel.SetDock( buttonRow, Dock.Bottom );
			body.Children.Add( buttonRow );

			string glyph = image switch
			{
				MessageBoxImage.Error => "✖",
				MessageBoxImage.Warning => "⚠",
				MessageBoxImage.Question => "?",
				MessageBoxImage.Information => "ℹ",
				_ => null
			};
			if ( glyph != null )
			{
				var icon = new TextBlock
				{
					Text = glyph,
					FontSize = 28,
					Margin = new Thickness( 0, 0, 16, 0 ),
					VerticalAlignment = VerticalAlignment.Top,
					Foreground = image == MessageBoxImage.Error ? Brushes.OrangeRed : image == MessageBoxImage.Warning ? Brushes.Orange : Brushes.LightSkyBlue
				};
				DockPanel.SetDock( icon, Dock.Left );
				body.Children.Add( icon );
			}
			body.Children.Add( new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 14, VerticalAlignment = VerticalAlignment.Center } );
			window.Content = body;

			if ( owner != null && owner.IsVisible )
			{
				await window.ShowDialog( owner );
			}
			else
			{
				var closed = new TaskCompletionSource();
				window.Closed += ( s, e ) => closed.TrySetResult();
				window.Show();
				await closed.Task;
			}
			return result;
		}
	}

	/// <summary>
	/// Native open/save dialogs (replacements for Microsoft.Win32.OpenFileDialog / SaveFileDialog)
	/// </summary>
	public static class FilePickers
	{
		public static readonly FilePickerFileType JimeFile = new FilePickerFileType( "Journey File" ) { Patterns = new[] { "*.jime" } };
		public static readonly FilePickerFileType ZipFile = new FilePickerFileType( "ZIP archive" ) { Patterns = new[] { "*.zip" } };

		/// <summary>
		/// returns the chosen full path, or null if cancelled
		/// </summary>
		public static Task<string> SaveJimeFile( Window owner, string title, string folder, string suggestedName )
		{
			return SaveFile( owner, title, folder, suggestedName, JimeFile, "jime" );
		}

		public static async Task<string> SaveFile( Window owner, string title, string folder, string suggestedName, FilePickerFileType type, string extension )
		{
			var provider = ( owner ?? App.ActiveWindow() )?.StorageProvider;
			if ( provider == null || !provider.CanSave )
				return null;

			var file = await provider.SaveFilePickerAsync( new FilePickerSaveOptions
			{
				Title = title,
				SuggestedStartLocation = await StartFolder( provider, folder ),
				SuggestedFileName = string.IsNullOrWhiteSpace( suggestedName ) ? null : Path.GetFileNameWithoutExtension( suggestedName ),
				DefaultExtension = extension,
				FileTypeChoices = new[] { type },
				ShowOverwritePrompt = true
			} );
			return file?.TryGetLocalPath();
		}

		/// <summary>
		/// returns the chosen full path, or null if cancelled
		/// </summary>
		public static Task<string> OpenJimeFile( Window owner, string title, string folder )
		{
			return OpenFile( owner, title, folder, JimeFile );
		}

		public static async Task<string> OpenFile( Window owner, string title, string folder, FilePickerFileType type )
		{
			var provider = ( owner ?? App.ActiveWindow() )?.StorageProvider;
			if ( provider == null || !provider.CanOpen )
				return null;

			var files = await provider.OpenFilePickerAsync( new FilePickerOpenOptions
			{
				Title = title,
				SuggestedStartLocation = await StartFolder( provider, folder ),
				AllowMultiple = false,
				FileTypeFilter = new[] { type }
			} );
			return files?.FirstOrDefault()?.TryGetLocalPath();
		}

		/// <summary>
		/// opens a folder in Explorer / Finder / the Linux file manager
		/// </summary>
		public static async Task OpenFolder( Window owner, string path )
		{
			var launcher = TopLevel.GetTopLevel( owner ?? App.ActiveWindow() )?.Launcher;
			if ( launcher == null || !await launcher.LaunchDirectoryInfoAsync( new DirectoryInfo( path ) ) )
				await MessageBox.Show( owner, "Could not open the folder:\r\n" + path, "Open Folder", MessageBoxButton.OK, MessageBoxImage.Warning );
		}

		static async Task<IStorageFolder> StartFolder( IStorageProvider provider, string folder )
		{
			if ( string.IsNullOrEmpty( folder ) || !Directory.Exists( folder ) )
				return null;
			return await provider.TryGetFolderFromPathAsync( folder );
		}
	}

	public static class WindowExtensions
	{
		/// <summary>
		/// dialog-style window: no minimize/maximize buttons (was a user32.dll call in the WPF editor)
		/// </summary>
		public static void HideMinimizeAndMaximizeButtons( this Window window )
		{
			window.CanMinimize = false;
			window.CanMaximize = false;
		}
	}
}
