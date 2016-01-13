using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace PowerPointPresentation
{
  /// <summary>
  /// Работа с FTP
  /// </summary>
  public class FTP
  {
    public const string FilesServerDir = "files";

    private readonly string _FTPHost;
    private readonly string _UserName;
    private readonly string _UserPassword;
    private readonly string _UploadImagesBaseDir;

    private List<string> _CreatedFtpDirs = new List<string>();

    /// <summary>
    /// Коллбэк на завершение загрузки картинки по FTP
    /// </summary>
    public event EventHandler<UploadImageCompliteInfo> UploadImageCompleteCallback;
    /// <summary>
    /// Событие загрузки блока презентации
    /// </summary>
    public event EventHandler<UploadPresentationBlockInfo> OnUploadPresentationBlockCallbak;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ftpHost">Имя FTP хоста</param>
    /// <param name="userName">Имя пользователя</param>
    /// <param name="userPassword">Пароль пользователя</param>
    /// <param name="uploadImagesBaseDirectory">Директория для загрузки фотографий</param>
    public FTP(string ftpHost, string userName, string userPassword, string uploadImagesBaseDirectory)
    {
      if (String.IsNullOrEmpty(ftpHost) || String.IsNullOrEmpty(userName))
        throw new ArgumentNullException(String.Format("неправильный формат имени хоста \"{0}\" или пользователя \"{1}\" FTP для экспорта", userName, userPassword));

      _FTPHost = ftpHost;
      _UserName = userName;
      _UserPassword = userPassword;
      _UploadImagesBaseDir = uploadImagesBaseDirectory;

      #region Содаю необходимые директории
      if (!String.IsNullOrEmpty(_UploadImagesBaseDir))
      {
        CreateFtpFolder(_UploadImagesBaseDir);
        CreateFtpFolder(GetUploadImagesServerFullDirectory());
      }
      else
      {
        CreateFtpFolder(FilesServerDir);
      }

      //CreateFtpFolder(Path.Combine(_UploadImagesBaseDir, SmallImageServerDir));
      //CreateFtpFolder(Path.Combine(_UploadImagesBaseDir, AverageAndBigImageServerDir));
      #endregion
    }

    private string GetUploadImagesServerFullDirectory()
    {
      if (String.IsNullOrWhiteSpace(_UploadImagesBaseDir))
        return FilesServerDir;
      else
        return _UploadImagesBaseDir + "/" + FilesServerDir;
    }

    /// <summary>
    /// Выложить картинки на FTP
    /// </summary>
    /// <param name="presInfo">Информация о презентации</param>
    public void UploadImages(PresentationInfo presInfo)
    {
      if (presInfo == null)
        throw new ArgumentNullException("presentation info is not set");

      for (int index = 0; index < presInfo.SlidersInfo.Count; index++)
      {
        SlideInfo slideInfo = presInfo.SlidersInfo[index];

        //if (!String.IsNullOrEmpty(slideInfo.ImageNameServerSmall))
        //{
        //  UploadImage(slideInfo.ImageNameClientSmall, String.Format("{0}/{1}", SmallImageServerDir, slideInfo.ImageNameServerSmall));
        //}

        CreateFtpFolder(Path.Combine(_UploadImagesBaseDir, FilesServerDir, presInfo.DbId.ToString()));

        if (!String.IsNullOrEmpty(slideInfo.ImageNameClientAverage))
        {
          CreateFtpFolder(Path.Combine(_UploadImagesBaseDir, FilesServerDir, presInfo.DbId.ToString(), "268"));
          UploadImage(
            Path.Combine(SlideInfo.GetLocalImageDirectoryAbsolutePath(presInfo.DbId, "268"), slideInfo.ImageNameClientAverage),
            String.Format("{0}/{1}/268/{2}", FilesServerDir, presInfo.DbId, slideInfo.ImageNameClientAverage));
        }

        if (!String.IsNullOrEmpty(slideInfo.ImageNameClientBig))
        {
          CreateFtpFolder(Path.Combine(_UploadImagesBaseDir, FilesServerDir, presInfo.DbId.ToString(), "653"));
          UploadImage(
            Path.Combine(SlideInfo.GetLocalImageDirectoryAbsolutePath(presInfo.DbId, "653"), slideInfo.ImageNameClientBig),
            String.Format("{0}/{1}/653/{2}", FilesServerDir, presInfo.DbId, slideInfo.ImageNameClientBig));
        }

        if (UploadImageCompleteCallback != null)
          UploadImageCompleteCallback(this, new UploadImageCompliteInfo { TotalImagesCount = presInfo.SlidersInfo.Count, CurrentImageNumber = index + 1 });
      }

      try
      {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(String.Format("ftp://{0}/{1}/{2}/{3}",
                                                                               _FTPHost,
                                                                               Path.Combine(_UploadImagesBaseDir, FilesServerDir),
                                                                               presInfo.DbId,
                                                                               presInfo.UrlNews + ".zip"));

        request.UseBinary = true;
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(_UserName, _UserPassword);

        Stream requestStream = request.GetRequestStream();

        using (FileStream sourse = new FileStream(presInfo.ZipPresentationAbsoluteLocation, FileMode.Open))
        {
          int count = 0;
          int lenght = 0;
          byte[] buffer = new byte[4096];
          while ((count = sourse.Read(buffer, 0, 4096)) != 0)
          {
            lenght += count;

            if (OnUploadPresentationBlockCallbak != null)
              OnUploadPresentationBlockCallbak(this, new UploadPresentationBlockInfo { PercentProgress = (int)(lenght * 100 / sourse.Length) });

            requestStream.Write(buffer, 0, count);
          }

          requestStream.Close();
        }

        File.Delete(presInfo.ZipPresentationAbsoluteLocation);
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("Во время загрузки файла с презентацией {0} на сервер, возникла ошибка: {1}", presInfo.ServerFileName, ex.Message));
      }
    }

    /// <summary>
    /// Загружает изображение на сервер
    /// </summary>
    /// <param name="uploadImagePath">Путь загрузки картинки на сервер с именем файла</param>
    /// <param name="clientImagePath">Путь к клиентсокой картинке с именем файла</param>
    private void UploadImage(string clientImagePath, string uploadImagePath)
    {
      if (String.IsNullOrEmpty(uploadImagePath) || String.IsNullOrEmpty(clientImagePath))
        throw new ArgumentNullException("upload image path or client image path is null");

      try
      {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(String.Format("ftp://{0}/{1}/{2}", _FTPHost, _UploadImagesBaseDir, uploadImagePath));

        request.UseBinary = true;
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(_UserName, _UserPassword);

        Stream requestStream = request.GetRequestStream();

        byte[] fileData = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), PPTFiles._PresentationImageDir, clientImagePath));
        requestStream.Write(fileData, 0, fileData.Length);

        requestStream.Close();

        // Удаляю картинку с клиента в случаи успешной отправки на сервер
        File.Delete(Path.Combine(Directory.GetCurrentDirectory(), PPTFiles._PresentationImageDir, clientImagePath));
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("Во время загрузки картинки {0} на Ftp {1} возникла ошибка: {2}", clientImagePath, uploadImagePath, ex.Message));
      }
    }

    private void CreateFtpFolder(string folderPath)
    {
      if (String.IsNullOrEmpty(folderPath))
        throw new ArgumentNullException("При создании папки на FTP путь к папке не может быть пустым");

      if (_CreatedFtpDirs.Contains(folderPath))
        return;

      _CreatedFtpDirs.Add(folderPath);

      try
      {
        WebRequest request = WebRequest.Create(String.Format("ftp://{0}/{1}", _FTPHost, folderPath.Replace("\\", "/")));
        request.Method = WebRequestMethods.Ftp.MakeDirectory;
        request.Credentials = new NetworkCredential(_UserName, _UserPassword);
        request.GetResponse();
      }
      catch (WebException ex)
      {
        FtpWebResponse response = (FtpWebResponse)ex.Response;
        if (response.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
          throw new Exception(String.Format("При создании папки {0} на FTP произошла ошибка: {1}", folderPath, ex.Message));
      }
    }
  }

  /// <summary>
  /// Информация о завершении загрузки картинки по FTP
  /// </summary>
  public class UploadImageCompliteInfo : EventArgs
  {
    /// <summary>
    /// Текущий порядковый номер картинки
    /// </summary>
    public int CurrentImageNumber { get; set; }
    /// <summary>
    /// Общее количество картинок
    /// </summary>
    public int TotalImagesCount { get; set; }
  }

  public class UploadPresentationBlockInfo : EventArgs
  {
    public int PercentProgress { get; set; }
  }
}
