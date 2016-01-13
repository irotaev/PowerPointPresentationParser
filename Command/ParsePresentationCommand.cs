using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace PowerPointPresentation.Command
{
  public class ParsePresentationCommand : ICommand
  {
    public ParsePresentationCommand(Action parsePresentationAction)
    {
      _parsePresentationAction = parsePresentationAction;
    }

    private readonly Action _parsePresentationAction;

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public event EventHandler CanExecuteChanged;

    public void Execute(object parameter)
    {
      _parsePresentationAction();
    }
  }
}
