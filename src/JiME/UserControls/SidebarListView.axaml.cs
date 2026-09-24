using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace JiME.UserControls
{
	/// <summary>
	/// Interaction logic for SidebarListView.xaml
	/// </summary>
	public partial class SidebarListView : UserControl
	{
		//styled properties so the {Binding Title}/{Binding ListData} update when the parent sets them after construction
		public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<SidebarListView, string>( nameof( Title ) );
		public static readonly StyledProperty<Array> ListDataProperty = AvaloniaProperty.Register<SidebarListView, Array>( nameof( ListData ) );

		public string Title
		{
			get => GetValue( TitleProperty );
			set => SetValue( TitleProperty, value );
		}
		public Array ListData
		{
			get => GetValue( ListDataProperty );
			set => SetValue( ListDataProperty, value );
		}

		public EventHandler onAddEvent, onRemoveEvent, onSettingsEvent;

		public SidebarListView()
		{
			InitializeComponent();
			DataContext = this;
		}

		private void AddInteraction_Click( object sender, RoutedEventArgs e )
		{
			onAddEvent?.Invoke( sender, e );
		}

		private void Settings_Click( object sender, RoutedEventArgs e )
		{
			onSettingsEvent?.Invoke( sender, e );
		}

		private void RemoveInteraction_Click( object sender, RoutedEventArgs e )
		{
			onRemoveEvent?.Invoke( sender, e );
		}
	}
}
