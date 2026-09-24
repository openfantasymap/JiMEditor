using Avalonia;
using Avalonia.Headless;
using JiME.Tests;

[assembly: AvaloniaTestApplication( typeof( HeadlessApp ) )]

namespace JiME.Tests
{
	/// <summary>
	/// Runs the real App (styles, resources, converters) on the headless platform with Skia rendering,
	/// so windows can be opened and captured as images
	/// </summary>
	public class HeadlessApp
	{
		public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
			.UseSkia()
			.UseHarfBuzz()
			.WithInterFont()
			.UseHeadless( new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false } );
	}
}
