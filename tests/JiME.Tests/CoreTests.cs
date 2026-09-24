using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace JiME.Tests
{
	/// <summary>
	/// The .jime format is shared with the Your Journey companion app, so it must not drift
	/// </summary>
	public class FileFormatTests
	{
		static FileFormatTests()
		{
			Utils.Init();
			Utils.LoadHexData();
		}

		[Fact]
		public void Vector_is_written_like_WPF_did()
		{
			Assert.Equal( "\"240,138.564064\"", JsonConvert.SerializeObject( new Vector( 240, 138.564064 ) ) );
			Assert.Equal( "\"-1,0.5\"", JsonConvert.SerializeObject( new Vector( -1, 0.5 ) ) );
		}

		[Theory]
		[InlineData( "\"240,138.564064\"" )]
		[InlineData( "\"240;138.564064\"" )]
		[InlineData( "{\"X\":240,\"Y\":138.564064}" )]
		public void Vector_reads_all_known_forms( string json )
		{
			Assert.Equal( new Vector( 240, 138.564064 ), JsonConvert.DeserializeObject<Vector>( json ) );
		}

		static Scenario SampleScenario()
		{
			Scenario s = new Scenario();
			s.scenarioName = "Round Trip";
			HexTile tile = new HexTile( 201 ) { angle = 60 };
			Token token = new Token( TokenType.Search ) { triggerName = "Some Event" };
			token.MoveTo( new Vector( 100, 120 ) );
			tile.tokenList.Add( token );
			s.chapterObserver[0].AddTile( tile );
			s.AddInteraction( new TextInteraction( "Some Event" ) { isTokenInteraction = true } );
			return s;
		}

		[Fact]
		public void Hex_tiles_and_tokens_keep_the_wpf_json_shape()
		{
			JObject json = JObject.Parse( new FileManager( SampleScenario() ).Serialize() );
			JObject tile = (JObject)json["chapters"][0]["tileObserver"][0];

			Assert.Equal(
				new[] { "GUID", "angle", "color", "flavorBookData", "hexRoot", "idNumber", "isStartTile", "position", "tileSide", "tileType", "tokenCount", "tokenList", "triggerName" },
				tile.Properties().Select( p => p.Name ).OrderBy( n => n, StringComparer.Ordinal ).ToArray() );
			Assert.Equal( JTokenType.String, tile["position"].Type );
			Assert.StartsWith( "240,138.564064", tile["position"].Value<string>() );
			Assert.Equal( JTokenType.String, tile["hexRoot"].Type );

			JObject token = (JObject)tile["tokenList"][0];
			Assert.Equal(
				new[] { "GUID", "dataName", "idNumber", "isEmpty", "personType", "position", "tokenType", "triggerName", "triggeredByName" },
				token.Properties().Select( p => p.Name ).OrderBy( n => n, StringComparer.Ordinal ).ToArray() );
			Assert.Equal( "100,120", token["position"].Value<string>() );
		}

		[Fact]
		public void Scenario_round_trips()
		{
			Scenario original = SampleScenario();
			Scenario loaded = FileManager.Deserialize( new FileManager( original ).Serialize(), "roundtrip.jime" );

			Assert.Equal( "Round Trip", loaded.scenarioName );
			Assert.Equal( original.scenarioGUID, loaded.scenarioGUID );
			HexTile a = (HexTile)original.chapterObserver[0].tileObserver[0];
			HexTile b = (HexTile)loaded.chapterObserver[0].tileObserver[0];
			Assert.Equal( a.position, b.position );
			Assert.Equal( a.hexRoot, b.hexRoot );
			Assert.Equal( 60, b.angle );
			Assert.Equal( new Vector( 100, 120 ), b.tokenList[0].position );
			Assert.Contains( loaded.interactionObserver, i => i.dataName == "Some Event" && i is TextInteraction );
		}

		[Fact]
		public void Token_stays_inside_the_tile_canvas()
		{
			Token t = new Token( TokenType.Person );
			Assert.False( t.MoveTo( new Vector( 10, 300 ) ) );
			Assert.False( t.MoveTo( new Vector( 300, 500 ) ) );
			Assert.True( t.MoveTo( new Vector( 25, 487 ) ) );
			Assert.Equal( new Vector( 25, 487 ), t.position );
		}

		[Fact]
		public void Hex_geometry_has_one_centre_per_hexagon()
		{
			foreach ( int id in Utils.LoadTiles() )
			{
				HexTile t = new HexTile( id );
				Assert.Equal( Utils.hexDictionary[id].tileCount, t.GetHexCenters().Length );
				Assert.DoesNotContain( t.GetHexCenters(), c => c.X == -1 || c.Y == -1 );
			}
		}
	}

	[Collection( "data folder" )]
	public class ProjectFolderTests : IDisposable
	{
		readonly string folder = Path.Combine( Path.GetTempPath(), "jime-tests-" + Guid.NewGuid().ToString( "N" ) );
		readonly string previous = Environment.GetEnvironmentVariable( AppPaths.OverrideVariable );

		public ProjectFolderTests()
		{
			Environment.SetEnvironmentVariable( AppPaths.OverrideVariable, folder );
			Utils.Init();
		}

		public void Dispose()
		{
			Environment.SetEnvironmentVariable( AppPaths.OverrideVariable, previous );
			if ( Directory.Exists( folder ) )
				Directory.Delete( folder, true );
		}

		[Fact]
		public void Override_variable_wins()
		{
			Assert.Equal( folder, AppPaths.BaseFolder );
			Assert.Equal( Path.Combine( folder, "abc" ), AppPaths.CampaignFolder( "abc" ) );
		}

		[Fact]
		public void Default_folder_is_in_Documents()
		{
			Environment.SetEnvironmentVariable( AppPaths.OverrideVariable, null );
			string expectedParent = OperatingSystem.IsWindows()
				? Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments )
				: Environment.GetEnvironmentVariable( "XDG_DOCUMENTS_DIR" ) is string xdg && xdg.Length > 0 && !OperatingSystem.IsMacOS()
					? xdg
					: Path.Combine( Environment.GetEnvironmentVariable( "HOME" ), "Documents" );
			Assert.Equal( Path.Combine( expectedParent, "Your Journey" ), AppPaths.BaseFolder );
		}

		[Fact]
		public void Project_list_ignores_os_junk_files()
		{
			Directory.CreateDirectory( folder );
			Scenario s = new Scenario() { scenarioName = "Listed" };
			new FileManager( s ) { fileName = "listed.jime" }.SaveToFolder( folder );
			File.WriteAllText( Path.Combine( folder, ".DS_Store" ), "junk" );
			File.WriteAllText( Path.Combine( folder, "._listed.jime" ), "junk" );
			File.WriteAllText( Path.Combine( folder, "notes.txt" ), "junk" );
			Directory.CreateDirectory( Path.Combine( folder, AppPaths.SavesFolderName ) );

			Campaign c = new Campaign { campaignName = "A Campaign" };
			Directory.CreateDirectory( AppPaths.CampaignFolder( c.campaignGUID.ToString() ) );
			File.WriteAllText( Path.Combine( AppPaths.CampaignFolder( c.campaignGUID.ToString() ), c.campaignGUID + ".json" ), JsonConvert.SerializeObject( c ) );

			var errors = 0;
			FileManager.ReportError = ( m, t ) => errors++;
			var items = FileManager.GetProjects().ToList();

			Assert.Equal( 0, errors );
			Assert.Equal( 2, items.Count );
			Assert.Contains( items, i => i.projectType == ProjectType.Standalone && i.Title == "Listed" );
			Assert.Contains( items, i => i.projectType == ProjectType.Campaign && i.Title == "A Campaign" );
		}
	}
}
