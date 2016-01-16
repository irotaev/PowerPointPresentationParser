using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.PowerPoint;

namespace PowerPointPresentation.Exceptions
{
  public class ExceptionLoger
  {
    public const string LOG_FOLDER = "Logs";
    public const string EXCEPTION_LOG_FILE = "exception_log.txt";

    public void WriteLog(string message)
    {
      try
      {
        if (!Directory.Exists(LOG_FOLDER))
          Directory.CreateDirectory(LOG_FOLDER);

        File.AppendAllText(Path.Combine(LOG_FOLDER, EXCEPTION_LOG_FILE), message);
      }
      catch (Exception ex)
      {
        throw new ApplicationException("Во время записи лога исключения в файл произошла непредвиденная ошибка. \n Это фатальная ошибка, обратитесь к программистам. \n\n Приложение будет закрыто.\n\n" +
                                       string.Format("Ошибка: {0}", ex.Message));
        System.Windows.Application.Current.Shutdown();
      }
    }
  }
}
