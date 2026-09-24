using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace JiME
{
	public enum ScenarioType { Journey, Battle }
	public enum InteractionType { Text, Threat, StatTest, Decision, Branch, Darkness, MultiEvent, Persistent, Conditional, Dialog, Replace, Reward }
	public enum MonsterType { Ruffian, GoblinScout, OrcHunter, OrcMarauder, Warg, HillTroll, Wight }
	public enum TileType { Hex, Battle }
	public enum ThreatAttributes { }//armor, elite, etc
	public enum ProjectType { Standalone, Campaign }
	public enum EditMode { Intro, Resolution, Objective, Flavor, Pass, Fail, Progress, Dialog, Special, Persistent, Story, Event }
	public enum EditorMode { Information, Threat, Decision, Test, Branch }
	public enum Ability { Might, Agility, Wisdom, Spirit, Wit, None }
	public enum TerrainToken { None, Pit, Mist, Barrels, Table, FirePit, Statue }
	public enum TokenType { Search, Person, Threat, Darkness, Exploration, None }
	public enum PersonType { Human, Elf, Hobbit, Dwarf }
	public enum HelpType { Token, Grouping, Enemies, Triggers }
	public enum DifficultyBias { Light, Medium, Heavy }


	public interface ITile
	{
		TileType tileType { get; set; }
		int idNumber { get; set; }
		Vector position { get; set; }
	}

	public interface ICommonData
	{
		Guid GUID { get; set; }
		string dataName { get; set; }
		bool isEmpty { get; set; }
		string triggerName { get; set; }
	}

	public interface IInteraction
	{
		string dataName { get; set; }
		Guid GUID { get; set; }
		InteractionType interactionType { get; set; }
		void RenameTrigger( string oldName, string newName );
		int loreReward { get; set; }
		int xpReward { get; set; }
		int threatReward { get; set; }
		bool isTokenInteraction { get; set; }
		string triggerName { get; set; }
		TokenType tokenType { get; set; }
		PersonType personType { get; set; }
	}

	public class Debug
	{
		public static void Log( object p )
		{
#if DEBUG
			Console.WriteLine( p );
#endif
		}
	}

	public class ProjectItem
	{
		public string Title { get; set; }
		public string Date { get; set; }
		public string Description { get; set; }
		public ProjectType projectType { get; set; }
		public string fileName { get; set; }
		public string fileVersion { get; set; }
	}

	public class CampaignItem
	{
		public string scenarioName { get; set; }
		///file NAME only, not path
		public string fileName { get; set; }
	}

	public class TileSorter : IComparer<int>
	{
		public int Compare( int x, int y )
		{
			return x.CompareTo( y );
		}
	}

	public class DefaultStats
	{
		public string name { get; set; }
		public int health { get; set; }
		public string speed { get; set; }
		public string damage { get; set; }
		public int armor { get; set; }
		public int sorcery { get; set; }
		public string special { get; set; }
	}

	public static class Utils
	{
		/// <summary>
		/// AKA "Engine Version" in the companion app
		/// Update this number every time the file format changes with new features
		/// </summary>
		public static string formatVersion = "1.9";
		public static string appVersion = "0.19-alpha";
		public static Dictionary<int, HexTileData> hexDictionary { get; set; } = new Dictionary<int, HexTileData>();
		public static Dictionary<int, HexTileData> hexDictionaryB { get; set; } = new Dictionary<int, HexTileData>();
		public static int tolerance = 25;
		public static double[] dragSnapX, dragSnapY;
		//hexSnap is used to convert local to absolute canvas coords (world)
		public static double[] hexSnapX, hexSnapY;//used in HexTileData.cs
		public static bool isLoaded = false;
		//public static GalleryTile[] galleryTilesA;
		//public static GalleryTile[] galleryTilesB;
		public static DefaultStats[] defaultStats;

		/// <summary>
		/// builds the canvas snap grids and loads the default enemy stats (tile images are loaded by the UI)
		/// </summary>
		public static void Init()
		{
			//create the hex grid snaps on canvas
			dragSnapX = new double[20];
			dragSnapY = new double[20];
			//grid snap is STAGGERED
			//x to x is NOT whole width of hex
			//1 hex is 64 wide
			//NEXT x grid is 32(radius)+16(HALF of radius)=48 units away
			for ( int i = 0; i < dragSnapX.Length; i++ )
				dragSnapX[i] = i * 48;
			//each y grid snap is just half a hex height away
			for ( int i = 0; i < dragSnapY.Length; i++ )
				dragSnapY[i] = 55.4256256d / 2 * i;//27.7128128

			//max hex area of any composite shape
			hexSnapX = new double[25];//20
			hexSnapY = new double[26];//21

			//hexSnapX = new double[8];
			//hexSnapY = new double[9];
			//double xx = 36d, yy = 30;//offset onto canvas
			double xx = -480, yy = -277.128128d;//offset onto canvas
			for ( int c = 0; c < hexSnapX.Length; c++ )
				hexSnapX[c] = xx + ( 48d * c );
			for ( int c = 0; c < hexSnapY.Length; c++ )
				hexSnapY[c] = yy + ( 55.4256256d / 2 * c );

			//load default enemy stats
			defaultStats = LoadDefaultStats().ToArray();
		}

		public static float RemapValue( float value, float low1, float high1, float low2, float high2 )
		{
			//value = Math.Clamp( value, low1, high1 );
			if ( low2 < high2 )
				return low2 + ( value - low1 ) * ( high2 - low2 ) / ( high1 - low1 );
			else
				return high2 + ( value - high1 ) * ( low2 - high2 ) / ( low1 - high1 );
		}

		public static bool WithinTolerance( this Vector value1, Vector value2, float tolerance )
		{
			return Math.Abs( value1.X - value2.X ) <= tolerance
				&& Math.Abs( value1.Y - value2.Y ) <= tolerance;
		}

		public static bool WithinTolerance( this double value1, double value2, float tolerance )
		{
			return Math.Abs( value1 - value2 ) <= tolerance;
		}

		/// <summary>
		/// returns tile ID data in an array
		/// </summary>
		public static List<int> LoadTiles()
		{
			var assembly = Assembly.GetExecutingAssembly();
			string resourceName = assembly.GetManifestResourceNames()
	.Single( str => str.Contains( ".tiles.json" ) );

			using ( Stream stream = assembly.GetManifestResourceStream( resourceName ) )
			using ( StreamReader reader = new StreamReader( stream ) )
			{
				string json = reader.ReadToEnd();
				var list = JObject.Parse( json );
				JToken token = list.SelectToken( "tiles" );
				List<int> ret = token.ToObject( typeof( List<int> ) ) as List<int>;
				TileSorter sorter = new TileSorter();
				ret.Sort( sorter );
				return ret;
			}
		}

		static List<DefaultStats> LoadDefaultStats()
		{
			var assembly = Assembly.GetExecutingAssembly();
			string resourceName = assembly.GetManifestResourceNames()
	.Single( str => str.Contains( ".enemy-defaults.json" ) );

			using ( Stream stream = assembly.GetManifestResourceStream( resourceName ) )
			using ( StreamReader reader = new StreamReader( stream ) )
			{
				string json = reader.ReadToEnd();
				var list = JObject.Parse( json );
				JToken token = list.SelectToken( "defaults" );
				List<DefaultStats> ret = token.ToObject( typeof( List<DefaultStats> ) ) as List<DefaultStats>;
				return ret;
			}
		}

		public static void LoadHexData()
		{
			if ( isLoaded )
				return;
			isLoaded = true;

			var assembly = Assembly.GetExecutingAssembly();
			foreach ( var item in new string[] { "A", "B" } )
			{
				string resourceName = assembly.GetManifestResourceNames()
.Single( str => str.Contains( ".hextiles" + item + ".json" ) );

				using ( Stream stream = assembly.GetManifestResourceStream( resourceName ) )
				using ( StreamReader reader = new StreamReader( stream ) )
				{
					string json = reader.ReadToEnd();
					var list = JObject.Parse( json );
					JToken token = list.SelectToken( "tiles" );
					List<HexTileData> ret = token.ToObject( typeof( List<HexTileData> ) ) as List<HexTileData>;
					foreach ( HexTileData data in ret )
					{
						data.Init();
						if ( item == "A" )
						{
							hexDictionary.Add( data.id, data );
						}
						else
						{
							hexDictionaryB.Add( data.id, data );
						}
					}
				}
			}
		}
	}

	public static class ObservableExtensions
	{
		/// <summary>
		/// Is the given trigger name used in this collection?
		/// </summary>
		public static Tuple<string, string> IsTriggerUsed<T>( this ObservableCollection<T> collection, string name )
		{
			foreach ( ICommonData item in collection )
			{
				if ( item.triggerName == name )
				{
					string n = collection.ToString();
					int idx = n.IndexOf( "[" );
					n = n.Substring( idx + 6, n.Length - idx - 7 );
					return new Tuple<string, string>( item.dataName, n );
				}
			}
			return null;
		}

		/// <summary>
		/// Return the data model with the given name
		/// </summary>
		public static ICommonData GetData<T>( this ObservableCollection<T> collection, string name )
		{
			foreach ( ICommonData item in collection )
			{
				if ( item.dataName == name )
					return item;
			}
			return null;
		}
	}

	public static class Extensions
	{
		public static T[] Fill<T>( this T[] arr, T value )
		{
			for ( int i = 0; i < arr.Length; i++ )
			{
				arr[i] = value;
			}
			return arr;
		}

		public static T FirstOr<T>( this IEnumerable<T> source, T alternate )
		{
			foreach ( T t in source )
				return t;
			return alternate;
		}
	}

	public class SimulatorData
	{
		public bool[] includedEnemies = new bool[7];
		public DifficultyBias difficultyBias;
		public float poolPoints;
	}

	public class MonsterSim
	{
		public string dataName { get; set; }
		public int health { get; set; }
		public int shieldValue { get; set; }
		public int sorceryValue { get; set; }
		//public int damage { get; set; }
		public string specialAbility
		{
			get
			{
				string ret = "";
				for ( int i = 0; i < modList.Count; i++ )
				{
					ret += modList[i];
					if ( i < Math.Max( 0, modList.Count - 1 ) )
						ret += ", ";
				}
				return ret;
			}
		}
		public int count { get; set; }
		public float cost { get; set; }
		public float singlecost { get; set; }
		public List<string> modList { get; set; } = new List<string>();
	}

	//public class DebugTraceListener : TraceListener
	//{
	//	public override void Write( string message )
	//	{
	//	}

	//	public override void WriteLine( string message )
	//	{
	//		Debug.Log( message );
	//		Debugger.Break();
	//	}
	//}
}
