using MahApps.Metro.Controls;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace PowerPointPresentation.Views
{
  /// <summary>
  /// Interaction logic for Settings.xaml
  /// </summary>
  public partial class SettingsWindow : MetroWindow
  {
    public SettingsWindow()
    {
      InitializeComponent();

      XDocument xmlDoc = XDocument.Load("Settings.xml");

      var dbRemoteHost = xmlDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBRemoteHost"));
      RemoteHost.Text = dbRemoteHost.Value;

      var dbName = xmlDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBName"));
      DBName.Text = dbName.Value;

      var dbUser = xmlDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBUser"));
      DBUser.Text = dbUser.Value;

      var dbPasswrord = xmlDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBPassword"));
      DBPassword.Password = dbPasswrord.Value;

      var ftpHost = xmlDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("Host"));
      FTPHost.Text = ftpHost.Value;

      var ftpUserName = xmlDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("UserName"));
      FTPUserName.Text = ftpUserName.Value;

      var ftpUserPassword = xmlDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("UserPassword"));
      FTPUserPassword.Password = ftpUserPassword.Value;

      var ftpImagesDir = xmlDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("ImagesDir"));
      FTPImagesDir.Text = ftpImagesDir.Value;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
      if (String.IsNullOrEmpty(RemoteHost.Text) || String.IsNullOrEmpty(DBName.Text) || String.IsNullOrEmpty(DBUser.Text)
        || String.IsNullOrEmpty(FTPHost.Text) || String.IsNullOrEmpty(FTPUserName.Text))
      {
        MessageBox.Show("Вы неправильно заполнили поля");
        return;
      }

      XDocument xmlDoc = XDocument.Load("Settings.xml");

      var dbRemoteHost = xmlDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBRemoteHost"));
      dbRemoteHost.Value = RemoteHost.Text;

      var dbName = xmlDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBName"));
      dbName.Value = DBName.Text;

      var dbUser = xmlDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBUser"));
      dbUser.Value = DBUser.Text;

      var dbPasswrord = xmlDoc.Root.Element(XName.Get("ExportDBInfo")).Element(XName.Get("DBPassword"));
      dbPasswrord.Value = DBPassword.Password;

      var ftpHost = xmlDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("Host"));
      ftpHost.Value = FTPHost.Text;

      var ftpUserName = xmlDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("UserName"));
      ftpUserName.Value = FTPUserName.Text;

      var ftpUserPassword = xmlDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("UserPassword"));
      ftpUserPassword.Value = FTPUserPassword.Password;

      var ftpImagesDir = xmlDoc.Root.Element(XName.Get("ExportFtpInfo")).Element(XName.Get("ImagesDir"));
      ftpImagesDir.Value = FTPImagesDir.Text;

      xmlDoc.Save("Settings.xml");

      Close();
    }
  }
}
