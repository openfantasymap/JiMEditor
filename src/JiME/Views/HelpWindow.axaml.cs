using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace JiME.Views
{
	public partial class HelpWindow : Window
	{
		public HelpWindow( HelpType helpType, int tab = 0 )
		{
			InitializeComponent();

			if ( helpType == HelpType.Token )
			{
				Title = "Help On Tokens and Events";
				tokenHelp.IsVisible = true;
				tokenHelp.Items.OfType<TabItem>().ToArray()[tab].IsSelected = true;
			}
			else if ( helpType == HelpType.Grouping )
			{
				Title = "Help On Token Interaction Groups";
				groupHelp.IsVisible = true;
				groupHelp.Items.OfType<TabItem>().ToArray()[tab].IsSelected = true;
			}
			else if ( helpType == HelpType.Enemies )
			{
				Title = "Help On Enemy Damage and Difficulty";
				threatHelp.IsVisible = true;
				threatHelp.Items.OfType<TabItem>().ToArray()[tab].IsSelected = true;
			}
			else if ( helpType == HelpType.Triggers )
			{
				Title = "Help On Triggers";
				triggerHelp.IsVisible = true;
				triggerHelp.Items.OfType<TabItem>().ToArray()[tab].IsSelected = true;
			}
		}

		//parameterless ctor for the XAML previewer
		public HelpWindow() : this( HelpType.Token ) { }

		private void okButton_Click( object sender, RoutedEventArgs e )
		{
			Close( true );
		}
	}
}
