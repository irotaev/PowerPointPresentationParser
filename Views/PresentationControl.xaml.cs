using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PowerPointPresentation.Control;
using PowerPointPresentation.PresentationControl;

namespace PowerPointPresentation.Views
{
  /// <summary>
  /// Interaction logic for PresentationControl.xaml
  /// </summary>
  public partial class PresentationControl : UserControl
  {
    public PresentationControl(MainWindow window, Dictionary<string, string> categories)
    {
      _window = window;
      ControlState = PresentationControlState.WaitingExecution;

      Categories = categories;

      InitializeComponent();

      PresentationFileName.Text = "Файл не выбран";
    }

    private readonly MainWindow _window;
    private string _presentationFullPath;
    public Dictionary<string, string> Categories { get; set; }

    internal PresentationControlState ControlState { get; set; }

    private void ButtonRemove_OnClick(object sender, RoutedEventArgs e)
    {
      _window.RemoveControl(this);
    }

    public PresentationData GetData()
    {
      return new PresentationData
      {
        PresentationControl = this,
        Category = CategorieComboBox.SelectedItem,
        PresentationFullPath = _presentationFullPath,
        PresentationName = PresentationName.Text
      };
    }

    public bool Validate(out string message)
    {
      var isValid = true;

      var data = GetData();

      message = string.Empty;

      if (string.IsNullOrWhiteSpace(data.PresentationFullPath))
      {
        isValid = false;
        message += "Необходимо выбрать файл с презентацией\n";
      }

      if (string.IsNullOrWhiteSpace(data.PresentationName))
      {
        isValid = false;
        message += "Необходимо заполнить название презентации\n";
      }

      if (data.Category == null)
      {
        isValid = false;
        message += "Необходимо указать категорию презентации\n";
      }

      if (!isValid)
        Border.BorderBrush = Brushes.DarkRed;

      return isValid;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

      string allFileFormatsString = String.Join(";", PPTFiles.SupportedPowerPointFileFormats.Concat(PPTFiles.SupportedArchiveFormats).Select(el => { return "*" + el; }));

      dialog.DefaultExt = ".ppt";
      dialog.Filter = String.Format("Презентация или архив power point|{0}", allFileFormatsString);

      bool? result = dialog.ShowDialog();

      if (result == true)
      {
        PresentationFileName.Text = System.IO.Path.GetFileName(dialog.FileName);
        _presentationFullPath = dialog.FileName;
      }
    }

    private void Border_OnGotFocus(object sender, RoutedEventArgs e)
    {
      Border.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#484A4A");
    }
  }
}
