using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JiME
{
	/// <summary>
	/// JSON serialization/deserialization
	/// </summary>
	public class FileManager
	{
		/// <summary>
		/// Set by the UI: shows a save dialog (title, start folder, suggested file name) and returns the chosen full path, or null if cancelled
		/// </summary>
		[JsonIgnore]
		public static Func<string, string, string, Task<string>> RequestSavePath;
		/// <summary>
		/// Set by the UI: reports an error (message, title)
		/// </summary>
		[JsonIgnore]
		public static Action<string, string> ReportError = ( message, title ) => Console.Error.WriteLine( title + ": " + message );

		/// <summary>
		/// This number is updated every time the file format changes with new features
		/// </summary>
		public Guid scenarioGUID { get; set; }
		public Guid campaignGUID { get; set; }
		public string specialInstructions { get; set; }
		public string fileVersion { get; set; }
		public string fileName { get; set; }
		public string saveDate { get; set; }
		public ProjectType projectType { get; set; }
		public string scenarioName { get; set; }
		public string objectiveName { get; set; }
		public int threatMax { get; set; }
		public bool threatNotUsed { get; set; }
		public bool scenarioTypeJourney { get; set; }
		public int loreReward { get; set; }
		public int loreStartValue { get; set; }
		public int xpReward { get; set; }
		public int shadowFear { get; set; }
		public bool useTileGraphics { get; set; }

		[JsonConverter( typeof( InteractionConverter ) )]
		public List<IInteraction> interactions { get; set; }
		public List<Trigger> triggers { get; set; }
		public List<Objective> objectives { get; set; }
		public List<TextBookData> resolutions { get; set; }
		public List<Threat> threats { get; set; }
		public List<Chapter> chapters { get; set; }
		public List<int> globalTiles { get; set; }
		public Dictionary<string, bool> scenarioEndStatus { get; set; } = new Dictionary<string, bool>();
		public TextBookData introBookData { get; set; }

		public FileManager()
		{

		}

		public FileManager( Scenario source )
		{
			scenarioGUID = source.scenarioGUID;
			campaignGUID = source.campaignGUID;
			specialInstructions = source.specialInstructions;
			fileName = source.fileName;
			fileVersion = Utils.formatVersion;
			saveDate = source.saveDate;
			loreReward = source.loreReward;
			loreStartValue = source.loreStartValue;
			xpReward = source.xpReward;
			shadowFear = source.shadowFear;
			useTileGraphics = source.useTileGraphics;

			interactions = source.interactionObserver.ToList();
			//skip saving campaign triggers
			triggers = source.triggersObserver.Where( x => !x.isCampaignTrigger ).ToList();//source.triggersObserver.ToList();
			objectives = source.objectiveObserver.ToList();
			resolutions = source.resolutionObserver.ToList();
			threats = source.threatObserver.ToList();
			chapters = source.chapterObserver.ToList();
			globalTiles = source.globalTilePool.ToList();
			scenarioEndStatus = source.scenarioEndStatus;

			introBookData = source.introBookData;
			projectType = source.projectType;
			scenarioName = source.scenarioName;
			objectiveName = source.objectiveName;
			threatMax = source.threatMax;
			threatNotUsed = source.threatNotUsed;
			scenarioTypeJourney = source.scenarioTypeJourney;
		}

		/// <summary>
		/// saves Scenario. Detects if scenario is part of campaign, saves to proper folder
		/// </summary>
		public Task<bool> Save()
		{
			if ( campaignGUID == Guid.Empty )
				return Save( false, AppPaths.BaseFolder );
			else
				return Save( false, AppPaths.CampaignFolder( campaignGUID.ToString() ) );
		}

		/// <summary>
		/// saves a NEW standalone scenario to base project folder
		/// </summary>
		public Task<bool> SaveAs()
		{
			return Save( true, AppPaths.BaseFolder );
		}

		/// <summary>
		/// saves a NEW scenario to campaign folder
		/// </summary>
		public Task<bool> SaveAs( string campaignFolder )
		{
			return Save( true, campaignFolder );
		}

		/// <summary>
		/// outFolder is the full path, excluding filename. Creates folder if it doesn't exist
		/// </summary>
		private async Task<bool> Save( bool saveAs, string outFolder )
		{
			string basePath = outFolder;

			if ( saveAs || string.IsNullOrEmpty( fileName ) )
			{
				try
				{
					Directory.CreateDirectory( basePath );
				}
				catch ( Exception e )
				{
					ReportError( "Could not create the Scenario project folder.\r\nTried to create: " + basePath + "\r\n\r\n" + e.Message, "App Exception" );
					return false;
				}

				if ( RequestSavePath == null )
					return false;
				string chosen = await RequestSavePath( saveAs ? "Save Project As" : "Save Project", basePath, string.IsNullOrEmpty( fileName ) ? scenarioName : fileName );
				if ( string.IsNullOrEmpty( chosen ) )
					return false;
				if ( !chosen.EndsWith( ".jime", StringComparison.OrdinalIgnoreCase ) )
					chosen += ".jime";
				//the companion app only finds scenarios in the project (or campaign) folder, so the chosen folder is only used for the name
				fileName = chosen;
			}

			return SaveToFolder( basePath );
		}

		/// <summary>
		/// writes the scenario to basePath/fileName without asking anything; fileName must be set
		/// </summary>
		public bool SaveToFolder( string basePath )
		{
			//just use the filename, not the whole path
			fileName = Path.GetFileName( fileName );
			saveDate = DateTime.Now.ToString( "M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture );

			try
			{
				Directory.CreateDirectory( basePath );
				File.WriteAllText( Path.Combine( basePath, fileName ), Serialize() );
			}
			catch ( Exception e )
			{
				ReportError( "Could not save the project file.\r\n\r\nException:\r\n" + e.Message, "App Exception" );
				return false;
			}
			return true;
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject( this, Formatting.Indented );
		}

		public static Scenario Deserialize( string json, string fileName )
		{
			var fm = JsonConvert.DeserializeObject<FileManager>( json );
			fm.fileName = fileName;
			return Scenario.CreateInstance( fm );
		}

		/// <summary>
		/// Supply the FULL PATH with the filename
		/// </summary>
		public static Scenario Load( string filename, bool reportErrors = true )
		{
			try
			{
				return Deserialize( File.ReadAllText( filename ), Path.GetFileName( filename ) );
			}
			catch ( Exception e )
			{
				if ( reportErrors )
					ReportError( "Could not load the project file.\r\n\r\nException:\r\n" + e.Message, "App Exception" );
				return null;
			}
		}

		/// <summary>
		/// Supply the filename ONLY, not the full path
		/// </summary>
		public static Scenario LoadProject( string filename )
		{
			string basePath = AppPaths.BaseFolder;

			//make sure the project folder exists
			if ( !Directory.Exists( basePath ) )
			{
				ReportError( "Could not find the scenario project folder.\r\nTried to find: " + basePath, "App Exception" );
				return null;
			}

			return Load( Path.Combine( basePath, filename ) );
		}

		/// <summary>
		/// supply the campaign base path and scenario filename
		/// </summary>
		public static Scenario LoadProjectFromPath( string basePath, string filename )
		{
			//make sure the folder exists
			if ( !Directory.Exists( basePath ) )
			{
				ReportError( "Could not find the campaign project folder.\r\nTried to find: " + basePath, "App Exception" );
				return null;
			}

			return Load( Path.Combine( basePath, filename ) );
		}

		/// <summary>
		/// Return ProjectItem info for scenarios and campaigns in Project folder
		/// </summary>
		public static IEnumerable<ProjectItem> GetProjects()
		{
			string basePath;
			try
			{
				basePath = AppPaths.EnsureBaseFolder();
			}
			catch ( Exception e )
			{
				ReportError( "Could not create the scenario project folder.\r\nTried to create: " + AppPaths.BaseFolder + "\r\n\r\n" + e.Message, "App Exception" );
				return new List<ProjectItem>();
			}

			List<ProjectItem> items = new List<ProjectItem>();
			DirectoryInfo di = new DirectoryInfo( basePath );
			//only .jime files - skips .DS_Store, macOS "._" resource forks etc
			FileInfo[] files = di.GetFiles().Where( file => file.Extension.Equals( ".jime", StringComparison.OrdinalIgnoreCase ) && !file.Name.StartsWith( "._" ) ).ToArray();
			//find campaigns
			foreach ( DirectoryInfo dInfo in di.GetDirectories() )
			{
				if ( !File.Exists( Path.Combine( dInfo.FullName, dInfo.Name + ".json" ) ) )
					continue;
				Campaign c = LoadCampaign( dInfo.Name );
				if ( c != null )
				{
					FileInfo fi = new FileInfo( Path.Combine( basePath, dInfo.Name, dInfo.Name + ".json" ) );
					ProjectItem pi = new ProjectItem();
					pi.projectType = ProjectType.Campaign;
					pi.Date = fi.LastWriteTime.ToString( "M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture );
					pi.Title = c.campaignName;
					pi.fileName = dInfo.Name;
					pi.fileVersion = c.fileVersion;
					items.Add( pi );
				}
			}

			//find scenario files
			foreach ( FileInfo fi in files )
			{
				Scenario s = Load( fi.FullName );
				if ( s != null )
					items.Add( new ProjectItem() { Title = s.scenarioName, projectType = s.projectType, Date = s.saveDate, fileName = fi.Name, fileVersion = s.fileVersion } );
			}
			return items;
		}

		public static Campaign LoadCampaign( string campaignGUID )
		{
			if ( campaignGUID == AppPaths.SavesFolderName )
				return null;

			string basePath = AppPaths.CampaignFolder( campaignGUID );
			try
			{
				string json = File.ReadAllText( Path.Combine( basePath, campaignGUID + ".json" ) );
				return JsonConvert.DeserializeObject<Campaign>( json, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Populate } );
			}
			catch ( Exception e )
			{
				ReportError( "Could not load the Campaign data.\r\n\r\nException:\r\n" + e.Message, "App Exception" );
				return null;
			}
		}
	}
}
