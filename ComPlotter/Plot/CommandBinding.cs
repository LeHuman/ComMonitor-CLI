using System;
using System.Windows.Input;

namespace ComPlotter.Plot
{
    public delegate void Command();

    public delegate bool Qualifier();

    internal class CommandBinding : ICommand
    {
        private readonly Qualifier canExecute;
        private readonly Command execute;

        public CommandBinding(Command execute)
        {
            this.execute = execute;
        }

        public CommandBinding(Command execute, Qualifier canExecute)
        {
            this.canExecute = canExecute;
            this.execute = execute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute();
        }

        public void Execute(object parameter)
        {
            execute();
        }
    }
}
