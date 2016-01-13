using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace PowerPointPresentation.Command
{
  public class AddPresentationCommand : ICommand
  {
    public AddPresentationCommand(Action addPresentationAction)
    {
      _addPresentationAction = addPresentationAction;
    }

    private readonly Action _addPresentationAction;

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public event EventHandler CanExecuteChanged;

    public void Execute(object parameter)
    {
      _addPresentationAction();
    }
  }
}
