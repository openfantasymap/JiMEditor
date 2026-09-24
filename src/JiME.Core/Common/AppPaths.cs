using System;
using System.IO;

namespace JiME
{
	/// <summary>
	/// Resolves the "Your Journey" project folder shared with the companion app, the same way on every OS:
	///   Windows  %USERPROFILE%\Documents\Your Journey
	///   macOS    ~/Documents/Your Journey
	///   Linux    $XDG_DOCUMENTS_DIR/Your Journey (default ~/Documents/Your Journey)
	/// Set the JIME_DATA_DIR environment variable to override the folder entirely.
	/// </summary>
	public static class AppPaths
	{
		public const string FolderName = "Your Journey";
		public const string SavesFolderName = "Saves";
		public const string OverrideVariable = "JIME_DATA_DIR";

		/// <summary>
		/// Full path of the "Your Journey" folder (not created)
		/// </summary>
		public static string BaseFolder
		{
			get
			{
				string custom = Environment.GetEnvironmentVariable( OverrideVariable );
				if ( !string.IsNullOrEmpty( custom ) )
					return custom;
				return Path.Combine( DocumentsFolder(), FolderName );
			}
		}

		public static string CampaignFolder( string campaignGUID )
		{
			return Path.Combine( BaseFolder, campaignGUID );
		}

		/// <summary>
		/// Creates the base folder if needed and returns it
		/// </summary>
		public static string EnsureBaseFolder()
		{
			string path = BaseFolder;
			Directory.CreateDirectory( path );
			return path;
		}

		static string DocumentsFolder()
		{
			if ( OperatingSystem.IsWindows() )
				return Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );

			//.NET and Mono disagree about MyDocuments on Unix ($HOME vs ~/Documents), so build it explicitly
			string xdg = Environment.GetEnvironmentVariable( "XDG_DOCUMENTS_DIR" );
			if ( !string.IsNullOrEmpty( xdg ) && !OperatingSystem.IsMacOS() )
				return xdg;

			string home = Environment.GetEnvironmentVariable( "HOME" );
			if ( string.IsNullOrEmpty( home ) )
				home = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );
			return Path.Combine( home, "Documents" );
		}
	}
}
