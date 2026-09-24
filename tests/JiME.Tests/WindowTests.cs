using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using JiME.UserControls;
using JiME.Views;
using Xunit;

namespace JiME.Tests
{
	/// <summary>
	/// Opens every window of the editor with sample data on the headless platform. Catches XAML/binding/
	/// ctor exceptions and, when JIME_SCREENSHOTS is set to a folder, saves a PNG of each window for review.
	/// </summary>
	[Collection( "data folder" )]
	public class WindowTests
	{
		static Scenario SampleScenario()
		{
			Scenario s = new Scenario();
			s.scenarioName = "Headless Test Scenario";
			HexTile tile = new HexTile( 201 ) { angle = 60 };
			tile.tokenList.Add( new Token( TokenType.Search ) { triggerName = "Search the ruins" } );
			s.chapterObserver[0].AddTile( tile );
			s.chapterObserver[0].AddTile( new HexTile( 100 ) );
			s.AddInteraction( new TextInteraction( "Search the ruins" ) { isTokenInteraction = true, tokenType = TokenType.Search } );
			s.AddInteraction( new ThreatInteraction( "Orc ambush" ) );
			s.AddTrigger( "Ruins searched" );
			s.threatObserver.Add( new Threat( "Rising darkness", 20 ) );
			return s;
		}

		public static IEnumerable<object[]> Windows()
		{
			Func<Scenario, Window>[] factories =
			{
				s => new MainWindow( s ),
				s => new ProjectWindow(),
				s => new ScenarioWindow( s ),
				s => new CampaignWindow(),
				s => new CampaignTriggerEditor( new Campaign() ),
				s => new ChapterPropertiesWindow( s, s.chapterObserver[0] ),
				s => new TileEditorWindow( s, s.chapterObserver[0] ),
				s => new TokenEditorWindow( (HexTile)s.chapterObserver[0].tileObserver[0], s ),
				s => new TilePoolEditorWindow( s, s.chapterObserver[0] ),
				s => new BattleTileEditor( s, BattleChapter() ),
				s => new GalleryWindow( s, 0 ),
				s => new TextEditorWindow( s, EditMode.Intro, s.introBookData ),
				s => new TriggerEditorWindow( s ),
				s => new ObjectiveEditorWindow( s, new Objective( "New Objective" ) ),
				s => new MonsterEditorWindow(),
				s => new EnemyCalculator( new SimulatorData() ),
				s => new HelpWindow( HelpType.Triggers ),
				s => new TextInteractionWindow( s ),
				s => new BranchInteractionWindow( s ),
				s => new ThreatInteractionWindow( s ),
				s => new TestInteractionWindow( s ),
				s => new DecisionInteractionWindow( s ),
				s => new MultiEventWindow( s ),
				s => new PersistentInteractionWindow( s ),
				s => new ConditionalInteractionWindow( s ),
				s => new DialogInteractionWindow( s ),
				s => new ReplaceTokenInteractionWindow( s ),
				s => new RewardInteractionWindow( s ),
			};
			return factories.Select( ( f, i ) => new object[] { i, f } );
		}

		static Chapter BattleChapter()
		{
			Chapter c = new Chapter( "Battle" );
			c.ToBattleTile();
			return c;
		}

		[AvaloniaTheory]
		[MemberData( nameof( Windows ) )]
		public void Window_opens_and_renders( int index, Func<Scenario, Window> factory )
		{
			Window window = factory( SampleScenario() );
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var frame = window.CaptureRenderedFrame();
			Assert.NotNull( frame );

			string folder = Environment.GetEnvironmentVariable( "JIME_SCREENSHOTS" );
			if ( !string.IsNullOrEmpty( folder ) )
			{
				Directory.CreateDirectory( folder );
#pragma warning disable CS0618 // PNG is the default encoding; the options overload adds nothing here
				frame.Save( Path.Combine( folder, $"{index:00}-{window.GetType().Name}.png" ) );
#pragma warning restore CS0618
			}
			window.Close();
		}

		[AvaloniaFact]
		public void Tile_editor_draws_each_tile()
		{
			Scenario s = SampleScenario();
			s.useTileGraphics = true;
			var window = new TileEditorWindow( s, s.chapterObserver[0] );
			window.Show();
			Dispatcher.UIThread.RunJobs();

			//outline + artwork per tile
			var canvas = window.FindControl<Canvas>( "canvas" );
			Assert.Equal( 4, canvas.Children.Count );
			window.Close();
		}
	}
}
