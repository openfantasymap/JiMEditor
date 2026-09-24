using System;
using System.Windows.Input;

namespace JiME
{
	/// <summary>
	/// minimal ICommand for key bindings (replaces WPF RoutedUICommand)
	/// </summary>
	public class RelayCommand : ICommand
	{
		readonly Action execute;

		public RelayCommand( Action execute )
		{
			this.execute = execute;
		}

		public event EventHandler CanExecuteChanged { add { } remove { } }
		public bool CanExecute( object parameter ) => true;
		public void Execute( object parameter ) => execute();
	}
}
