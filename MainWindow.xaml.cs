using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;
using PowerPointPresentation.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup.Localizer;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using PowerPointPresentation.Command;
using PowerPointPresentation.Control;
using PowerPointPresentation.Exceptions;
using PowerPointPresentation.PresentationControl;
using PowerPointPresentation.Transport;

namespace PowerPointPresentation
{
  public partial class MainWindow : MetroWindow
  {

    private List<PowerPointPresentation.Views.PresentationControl> _presentationControls = new List<PowerPointPresentation.Views.PresentationControl>();
    private Dictionary<string, string> _Categories = new Dictionary<string, string>();

    private string _PresentationFullPath;
    private readonly ExceptionLoger _exceptionLoger = new ExceptionLoger();

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);

      InputBindings.Add(new KeyBinding(new AddPresentationCommand(() => { AddControl(); UpdateControlList(); }), new KeyGesture(Key.D, ModifierKeys.Control)));
      InputBindings.Add(new KeyBinding(new ParsePresentationCommand(() => ParsePresentations()), new KeyGesture(Key.S, ModifierKeys.Control)));

      UpdateControlList();

      Categortie.LoadFromFile();

      _Categories.Add("NA", Categortie.Categories["NA"]);
      List<string> allCAtegories = Categortie.Categories.Keys.ToList();
      allCAtegories.Remove("NA");

      foreach (string category in allCAtegories)
      {
        _Categories.Add(category, Categortie.Categories[category]);
      }
    }

    public void ParsePresentations()
    {
      #region Лицензия

      if (DateTime.UtcNow > new DateTime(2016, 01, 28, 23, 59, 59))
      {
        MessageBox.Show("Ваша лицензия истекла");
        Application.Current.Shutdown();
      }

      #endregion

      if (string.IsNullOrWhiteSpace(Login.Text))
      {
        LoginGroupBox.BorderBrush = Brushes.IndianRed;
        LoginGroupBox.Background = Brushes.IndianRed;
        return;
      }

      List<PowerPointPresentation.Views.PresentationControl> controls;
      lock (_presentationControls)
      {
        controls = _presentationControls.Where(c => c.ControlState == PresentationControlState.WaitingExecution).ToList();
        controls.ForEach(c => c.ControlState = PresentationControlState.InProgress);
      }

      UpdateControlList();

      foreach (var control in controls)
      {
        string validMessage;
        if (!control.Validate(out validMessage))
        {
          control.ControlState = PresentationControlState.WaitingExecution;
          UpdateControlList();
          continue;
        }

        var data = control.GetData();
        data.Login = Login.Text;

        try
        {
          ParsePresentation(data);
        }
        catch (Exception ex)
        {
          Debug.WriteLine(string.Format("Во время парсинга презентации [{0}] произошла непредвиденная ошибка", ex.Message));
        }
      }
    }

    public MainWindow()
    {
      InitializeComponent();

      #region Проверка лицензии
      //try
      //{
      //  //using (var licenseVerifier = new PowerPointPresentation.Lib.LicenseVerifier())
      //  //{
      //  //    if (!licenseVerifier.CheckLicense())
      //  //    {
      //  //        MessageBox.Show(String.Format("Ваша лицензия не активна\nВозможно Вам необходимо продлить лицензию"));
      //  //        Application.Current.Shutdown();
      //  //    }
      //  //}

      //  InternetTime.SNTPClient sntp = new InternetTime.SNTPClient("ntp1.ja.net");
      //  sntp.Connect(false); // true to update local client clock
      //  DateTime dt = sntp.DestinationTimestamp.AddMilliseconds(sntp.LocalClockOffset);

      //  if (dt > DateTime.ParseExact("01/09/2015", "d", System.Globalization.CultureInfo.InvariantCulture))
      //  {
      //    MessageBox.Show("Срок лицензии истек");
      //    Application.Current.Shutdown();
      //  }
      //}
      //catch
      //{
      //  MessageBox.Show("Во время обращения к серверу проверки лицензии произошла ошибка");
      //  Application.Current.Shutdown();
      //}
      #endregion
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
      ParsePresentations();
    }

    void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
      var status = (ParseProgressStatus)e.UserState;

      status.PresentationControl.ProgressInfo.Visibility = System.Windows.Visibility.Collapsed;
      status.PresentationControl.ProgressBar.Visibility = System.Windows.Visibility.Collapsed;

      if (status.PresentationControl.ProgressInfo.Visibility != System.Windows.Visibility.Visible)
        status.PresentationControl.ProgressInfo.Visibility = System.Windows.Visibility.Visible;

      if (!status.IsOnlyMessage && status.PresentationControl.ProgressBar.Visibility != System.Windows.Visibility.Visible)
        status.PresentationControl.ProgressBar.Visibility = System.Windows.Visibility.Visible;

      status.PresentationControl.ProgressInfo.Text = String.Format("{0}", status.Message);
      status.PresentationControl.ProgressBar.Value = e.ProgressPercentage;
    }

    private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      if (e.Error != null)
      {
        MessageBox.Show(String.Format("Во время обработки презентации произошла фатальная ошибка \n\n Постарайтесь запомнить условия возникновения ошибки, и обратитесь к программистам, чтобы они устранили неисправность. \n\n Ошибка:\n {0}.", e.Error.Message));
        Application.Current.Shutdown();
      }

      ParseProgressStatus status = (ParseProgressStatus)e.Result;

      status.PresentationControl.ProgressInfo.Visibility = System.Windows.Visibility.Collapsed;
      status.PresentationControl.ProgressBar.Visibility = System.Windows.Visibility.Collapsed;
      status.PresentationControl.PresentationGrid.Opacity = 1;
      status.PresentationControl.PresentationGrid.IsEnabled = true;

      // Всплывающее собщение, что парсинг прошел успешно
      MessagePopUp.Text = !status.IsError ? "Парсинг прошел успешно" : "Ошибка парсинга презентации";
      MessagePopUp.Background = !status.IsError ? Brushes.Green : Brushes.IndianRed;
      //MessagePopUp.Visibility = System.Windows.Visibility.Visible;
      Storyboard messagePopUp = (Storyboard)TryFindResource("StoryboardMessagePopUp");
      messagePopUp.Begin();

      RemoveControl(status.PresentationControl);
    }

    private void worker_DoWork(object sender, DoWorkEventArgs e)
    {
      WorkerArgument argument = (WorkerArgument)e.Argument;

      var progressStatus = new ParseProgressStatus { PresentationControl = argument.PresentationControl };

      try
      {
        #region Парсинг презентации

        ((BackgroundWorker)sender).ReportProgress(0,
          new ParseProgressStatus
          {
            PresentationControl = argument.PresentationControl,
            Message = "Начало парсинга презентации",
            IsOnlyMessage = true
          });

        PresentationInfo presInfo = null;
        MySQLPresentationTable abstractpresTable = null;
        using (PPTFiles pptFiles = new PPTFiles())
        {
          #region Получаюданные настройки соединения с БД

          string dbRemoteHost = null,
            dbName = null,
            dbUser = null,
            dbPassword = null;

          try
          {
            XDocument xmlDBDoc = XDocument.Load("Lib\\FCashProfile.tss");

            var XdbRemoteHost = xmlDBDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBRemoteHost"));
            dbRemoteHost = XdbRemoteHost.Value;

            var XdbName = xmlDBDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBName"));
            dbName = XdbName.Value;

            var XdbUser = xmlDBDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBUser"));
            dbUser = XdbUser.Value;

            var XdbPassword = xmlDBDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBPassword"));
            dbPassword = XdbPassword.Value;
          }
          catch (Exception ex)
          {
            throw new Exception(
              String.Format("Не получилось получить конфигурационные данные из файла конфигурации: {0}", ex.Message));
          }

          if (!String.IsNullOrEmpty(argument.UrlNews))
            presInfo.UrlNews = argument.UrlNews;


          if (String.IsNullOrEmpty(dbRemoteHost) || String.IsNullOrEmpty(dbName) || String.IsNullOrEmpty(dbUser))
            throw new Exception(
              "У вас не заполнена конфигурация соединения с базой данных для экспорта\nПожалуйста заполните ее через настройки");

          MySQLPresentationTable presTable = new MySQLPresentationTable(dbRemoteHost, dbName, dbUser, dbPassword);
          abstractpresTable = presTable;

          #endregion

          pptFiles.ParseSlideCompleteCallback += (object pptFile, SlideCompleteParsingInfo slideParsingInfo) =>
          {
            ((BackgroundWorker)sender).ReportProgress(
              (int)((decimal)slideParsingInfo.SlideCurrentNumber / (decimal)slideParsingInfo.SlideTotalNumber * 100),
              new ParseProgressStatus
              {
                PresentationControl = argument.PresentationControl,
                Message = "Обработка слайдов"
              });
          };

          presTable.CreateTable();

          presInfo = pptFiles.ExtractInfo(argument.PresentationFullPath, presTable);
          presInfo.Name = argument.PresentationName;
          presInfo.Title = argument.PresentationTitle;
          presInfo.Login = argument.Login;
          presInfo.Categorie = ((KeyValuePair<string, string>)argument.SelectedItem);
        }

        #endregion

        #region Заливка информации по презентации в БД

        {
          ((BackgroundWorker)sender).ReportProgress(0,
            new ParseProgressStatus
            {
              PresentationControl = argument.PresentationControl,
              Message = "Обновление данных на сервере",
              IsOnlyMessage = true
            });

          abstractpresTable.PutDataOnServer(presInfo);
        }

        #endregion

        #region Отправка на FTP

        try
        {
          ((BackgroundWorker)sender).ReportProgress(0,
            new ParseProgressStatus
            {
              PresentationControl = argument.PresentationControl,
              Message = "Подготовка к отправке файлов на FTP",
              IsOnlyMessage = true
            });

          XDocument xmlFtpDoc = XDocument.Load("Lib\\FCashProfile.tss");

          var ftpHost = xmlFtpDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("Host"));
          var ftpUserName = xmlFtpDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("UserName"));
          var ftpUserPassword = xmlFtpDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("UserPassword"));
          var ftpImagesDir = xmlFtpDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("ImagesDir"));

          FTP ftp = new FTP(ftpHost.Value, ftpUserName.Value, ftpUserPassword.Value, ftpImagesDir.Value);
          ftp.UploadImageCompleteCallback += (object ftpSender, UploadImageCompliteInfo completeInfo) =>
          {
            ((BackgroundWorker)sender).ReportProgress(
              (int)((decimal)completeInfo.CurrentImageNumber / (decimal)completeInfo.TotalImagesCount * 100),
              new ParseProgressStatus
              {
                PresentationControl = argument.PresentationControl,
                Message = "Загрузка изображений на FTP"
              });
          };

          ftp.OnUploadPresentationBlockCallbak += (object ftpSender, UploadPresentationBlockInfo blockInfo) =>
          {
            ((BackgroundWorker)sender).ReportProgress(blockInfo.PercentProgress,
              new ParseProgressStatus
              {
                PresentationControl = argument.PresentationControl,
                Message = "Загрузка презентации"
              });
          };

          List<string> imageNames = new List<string>();

          foreach (var slideInfo in presInfo.SlidersInfo)
          {
            if (!String.IsNullOrEmpty(slideInfo.ImageNameClientSmall))
              imageNames.Add(slideInfo.ImageNameClientSmall);

            if (!String.IsNullOrEmpty(slideInfo.ImageNameClientAverage))
              imageNames.Add(slideInfo.ImageNameClientAverage);

            if (!String.IsNullOrEmpty(slideInfo.ImageNameClientBig))
              imageNames.Add(slideInfo.ImageNameClientBig);
          }

          ftp.UploadImages(presInfo);
        }
        catch (Exception ex)
        {
          throw new Exception(String.Format("Во время отправки изображений на FTP возникла ошибка: {0}", ex.Message));
        }

        #endregion
      }
      catch (Exception ex)
      {
        lock (_exceptionLoger)
        {
          _exceptionLoger.WriteLog(string.Format(Environment.NewLine + Environment.NewLine + "[{0}] Во время обработки презентации [{1}] произошла ошибка.\r\n Ошибка: {2} \r\nСтек вызова: {3}", DateTime.Now, argument.PresentationName, ex.Message, ex.StackTrace));
        }

        progressStatus.IsError = true;
      }
      finally
      {
        e.Result = progressStatus;
      }
    }

    private class WorkerArgument
    {
      public PowerPointPresentation.Views.PresentationControl PresentationControl { get; set; }
      public string PresentationFullPath { get; set; }
      public string PresentationName { get; set; }
      public string PresentationTitle { get; set; }
      public object SelectedItem { get; set; }
      public string UrlNews { get; set; }
      public string Login { get; set; }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      SettingsWindow settingsWindow = new SettingsWindow();
      settingsWindow.ShowDialog();
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
      AddControl();

      UpdateControlList();
    }

    private void UpdateControlList()
    {
      #region Текст кнопки распарсить презенетации
      int count;
      lock (_presentationControls)
      {
        count = _presentationControls.Where(c => c.ControlState == PresentationControlState.WaitingExecution).Count();
      }

      ParsePresentationButton.Content = "Распарсить презентации (" + count.ToString() + ")";
      #endregion


      #region Сортировка списка

      var controls = PresentationPanel.Children.Cast<PowerPointPresentation.Views.PresentationControl>().ToList();
      PresentationPanel.Children.RemoveRange(0, PresentationPanel.Children.Count);

      controls = controls.OrderBy(c => c.ControlState).ToList();
      foreach (var control in controls)
      {
        PresentationPanel.Children.Add(control);
      }
      #endregion
    }

    private void ParsePresentation(PresentationData presData)
    {
      BackgroundWorker worker = new BackgroundWorker();
      worker.DoWork += worker_DoWork;
      worker.RunWorkerCompleted += worker_RunWorkerCompleted;
      worker.WorkerReportsProgress = true;
      worker.ProgressChanged += worker_ProgressChanged;

      WorkerArgument workerArgument = new WorkerArgument
      {
        PresentationControl = presData.PresentationControl,
        PresentationName = presData.PresentationName,
        PresentationFullPath = presData.PresentationFullPath,
        //PresentationTitle = string.Empty,
        SelectedItem = presData.Category,
        //UrlNews = UrlNews.Text,
        Login = presData.Login
      };

      presData.PresentationControl.PresentationGrid.Opacity = 0.2;
      presData.PresentationControl.PresentationGrid.IsEnabled = false;

      worker.RunWorkerAsync(workerArgument);
    }

    #region Control operations

    internal void AddControl()
    {
      var control = new PowerPointPresentation.Views.PresentationControl(this, _Categories);

      Brush brush;
      lock (_presentationControls)
      {
        brush = _presentationControls.Count % 2 == 0
          ? (Brush)new BrushConverter().ConvertFrom("#BDBCB6")
          : (Brush)new BrushConverter().ConvertFrom("#D2F5FD");
      }

      control.FindChildren<GroupBox>().ToList().ForEach(c => { c.Background = brush; c.BorderBrush = brush; });

      PresentationPanel.Children.Add(control);

      lock (_presentationControls)
      {
        _presentationControls.Add(control);
      }
    }

    internal void RemoveControl(PowerPointPresentation.Views.PresentationControl control)
    {
      lock (_presentationControls)
      {
        if (!_presentationControls.Contains(control)) return;

        _presentationControls.Remove(control);
      }

      PresentationPanel.Children.Remove(control);

      #region Обновить цвета контролов

      lock (_presentationControls)
      {
        for (int i = 0; i < _presentationControls.Count; i++)
        {
          var brush = i % 2 == 0
          ? (Brush)new BrushConverter().ConvertFrom("#BDBCB6")
          : (Brush)new BrushConverter().ConvertFrom("#D2F5FD");

          _presentationControls[i].FindChildren<GroupBox>().ToList().ForEach(c => { c.Background = brush; c.BorderBrush = brush; });
        }
      }

      #endregion

      UpdateControlList();
    }
    #endregion

    private void LoginGroupBox_OnGotFocus(object sender, RoutedEventArgs e)
    {
      LoginGroupBox.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#BDBCB6");
      LoginGroupBox.Background = (Brush)new BrushConverter().ConvertFrom("#BDBCB6");
    }
  }
}
