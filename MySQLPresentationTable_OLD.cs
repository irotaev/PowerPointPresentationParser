using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryAnalysis.UnidecodeSharp;
using System.Security;

namespace PowerPointPresentation
{
  /// <summary>
  /// Таблица презентации в базе данных
  /// </summary>
  public class MySQLPresentationTable_OLD : AbstractDBTable
  {    
    /// <summary>
    /// Создать объект таблицы презентации
    /// </summary>
    /// <param name="hostName">Имя хоста</param>
    /// <param name="dbName">Имя базы данных</param>
    /// <param name="userName">Имя пользователя</param>
    /// <param name="userPassword">Пароль пользователя</param>
    public MySQLPresentationTable_OLD(string hostName, string dbName, string userName, string userPassword) : base(hostName, dbName, userName, userPassword) { }

    protected const string _MAIN_TABLE_NAME = "main";

    #region Заполняется таблица main
    private string FormatIframeBDColumn(PresentationInfo presInfo)
    {
      string result = null;

      foreach (string imageName in presInfo.SlidersInfo.Select(pI => pI.ImageNameServerBig))
      {
        if (!String.IsNullOrEmpty(imageName))
          result += String.Format("<img src=\"/{0}/{1}/500/{2}\" class=\"slide\"/>", FTP.AverageAndBigImageServerDir, presInfo.DbId, imageName);
      }

      return result;
    }

    private void PutDataToMainTable(PresentationInfo presInfo)
    {
      if (presInfo == null)
        throw new ArgumentNullException("presInfo");

      #region Создание таблицы main, если это необходимо
      try
      {
        {
          MySqlCommand command = _MySqlConnection.CreateCommand();
          command.CommandText = String.Format(@"CREATE TABLE IF NOT EXISTS `{0}`
                              (
                              `id` MEDIUMINT NOT NULL,
                              `url_news` VARCHAR(300) NOT NULL,
                              `nazvanie` VARCHAR(300) NULL,
                              `category` VARCHAR(300) NOT NULL,
                              `iframe` MEDIUMTEXT NULL,
                              `url_dowload` TEXT(1000) NOT NULL,
                              `lastSmallImageIndex` INT NULL,
                              UNIQUE KEY (lastSmallImageIndex, id)
                              ) CHARSET={1}", SecurityElement.Escape(_MAIN_TABLE_NAME), TABLE_CHARSET);
          command.ExecuteNonQuery();
        }
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("Во время создания таблицы 'main' произошла ошибка: {1}", ex.Message));
      }

      #endregion

      #region Получаю данные из таблицы, необходимые для доформирования информации о презентации
      #region Получаю информацию о предыдущей презентации
      try
      {
        MySqlCommand command = _MySqlConnection.CreateCommand();
        command.CommandText = String.Format("SELECT * FROM `{0}` ORDER BY id DESC LIMIT 1", _MAIN_TABLE_NAME);
        var reader = command.ExecuteReader();

        int lastPresentationId = 0,
            lastSmallImageIndex = 0,
            lastAverageImageIndex = 0,
            lastBigImageIndex = 0;

        if (reader.HasRows)
        {
          reader.Read();

          Int32.TryParse(reader.GetString("lastSmallImageIndex"), out lastSmallImageIndex);
          Int32.TryParse(reader.GetString("id"), out lastPresentationId);
        }

        reader.Close();

        presInfo.DbId = ++lastPresentationId;

        presInfo.SlidersInfo.ForEach(info =>
        {
          if (!String.IsNullOrEmpty(info.ImageNameClientBig))
            info.ImageNameServerBig = ++lastBigImageIndex + ".jpg";

          if (!String.IsNullOrEmpty(info.ImageNameClientAverage))
            info.ImageNameServerAverage = ++lastAverageImageIndex + ".jpg";

          if (!String.IsNullOrEmpty(info.ImageNameClientSmall))
            info.ImageNameServerSmall = ++lastSmallImageIndex + ".png";
        });

        presInfo.LastImageSmallIndex = lastSmallImageIndex;


      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("При получении данных из таблицы {0}, необходимых для формирования информации по экспорту новой презентации, произошла ошибка: {1}", _MAIN_TABLE_NAME, ex.Message));
      }
      #endregion

      #region Формирую уникальный url_news
      try
      {
        int coincidenceCount = 0;
        string newNameAsTranslit;
        bool isNeedNextLoop = true;

        do
        {
          if (coincidenceCount == 0)
            newNameAsTranslit = presInfo.NameAsTranslit;
          else
            newNameAsTranslit = presInfo.NameAsTranslit + coincidenceCount;

          MySqlCommand command = _MySqlConnection.CreateCommand();
          command.CommandText = String.Format("SELECT id FROM {0} WHERE url_news='{1}'", _MAIN_TABLE_NAME, newNameAsTranslit);

          using (var reader = command.ExecuteReader())
          {
            if (reader.HasRows)
              coincidenceCount++;
            else
              isNeedNextLoop = false;
          }
        } while (isNeedNextLoop);

        presInfo.NameAsTranslit = newNameAsTranslit;
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("При формировании уникального url_new произошла ошибка: {0}", ex.Message));
      }
      #endregion
      #endregion

      try
      {
        MySqlCommand command = _MySqlConnection.CreateCommand();
        command.CommandText = String.Format(@"INSERT INTO `{7}` (`id`, `url_news`, `nazvanie`, `category`, `iframe`, `url_dowload`, `lastSmallImageIndex`) VALUES ({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')",
          presInfo.DbId, SecurityElement.Escape(presInfo.UrlNews), SecurityElement.Escape(presInfo.Name), SecurityElement.Escape(presInfo.Categorie.ToString().Unidecode()),
          SecurityElement.Escape(FormatIframeBDColumn(presInfo)), SecurityElement.Escape(presInfo.UrlDownload), presInfo.LastImageSmallIndex, SecurityElement.Escape(_MAIN_TABLE_NAME));

        command.ExecuteNonQuery();
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("Во время заполнения таблицы 'main' презентацией '{0}' произошла ошибка: {1}", presInfo.Name, ex.Message));
      }
    }
    #endregion

    #region Заполняется таблица, соответствующая конкретной презентации
    private string FormContentDbColumn(PresentationInfo presInfo)
    {
      string result = null;

      for (int index = 0; index < presInfo.SlidersInfo.Count; index++)
      {
        string className = index == 0 ? "es" : "as";

        result += String.Format("<tr>" +
                                 "<td class='{0}' colspan='2'>Слайд №{1}</td>" +
                                 "</tr>" +
                                 "<tr>" +
                                   "<td class='sludes'><img src='/{2}/{3}/225/{4}' /></td>" +
                                   " <td class='textaes'>{5}</td>" +
                                 "</tr>", className, (index + 1), FTP.AverageAndBigImageServerDir, presInfo.DbId, presInfo.SlidersInfo[index].ImageNameServerAverage, presInfo.SlidersInfo[index].Text);
      }

      return result;
    }

    private void PutDataToConcretePresentationTable(PresentationInfo presInfo)
    {
      if (presInfo == null)
        throw new ArgumentNullException("presInfo");

      #region Создаю конкретную таблицу, если она еще не создана
      try
      {
        MySqlCommand command = _MySqlConnection.CreateCommand();
        command.CommandText = String.Format(@"CREATE TABLE IF NOT EXISTS `{0}`
                              (
                              `id` MEDIUMINT NOT NULL AUTO_INCREMENT,
                              `url_news` VARCHAR(200) NOT NULL,
                              `title` VARCHAR(200) NULL,
                              `nazvanie` VARCHAR(200) NOT NULL,
                              `slider` TEXT NULL,
                              `url_dowload` TEXT(1000) NOT NULL,
                              `size` FLOAT NOT NULL,
                              `content` TEXT NULL,
                              `iframe` INT NOT NULL,
                              `random` TEXT NULL,
                              `poxpres` TEXT NULL,
                              PRIMARY KEY (id)
                              ) CHARSET={1}", SecurityElement.Escape(presInfo.Categorie.ToString()), TABLE_CHARSET);
        command.ExecuteNonQuery();
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("Во время создания таблицы '{0}' произошла ошибка: {1}", presInfo.NameAsTranslit, ex.Message));
      }
      #endregion

      try
      {
        MySqlCommand command = _MySqlConnection.CreateCommand();
        command.CommandText = String.Format(new System.Globalization.CultureInfo("en-GB"), @"
          INSERT INTO `{8}` (`url_news`, `title`, `nazvanie`, `slider`, `url_dowload`, `size`, `content`, `iframe`)
           VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', {5:0.00}, '{6}', '{7}')
        ", SecurityElement.Escape(presInfo.UrlNews), SecurityElement.Escape(presInfo.Title), SecurityElement.Escape(presInfo.Name), SecurityElement.Escape(FormatIframeBDColumn(presInfo)),
         SecurityElement.Escape(presInfo.UrlDownload), Convert.ToSingle(presInfo.FileSize / 1024 / 1024, System.Globalization.CultureInfo.InvariantCulture),
         SecurityElement.Escape(FormContentDbColumn(presInfo)), presInfo.DbId, SecurityElement.Escape(presInfo.Categorie.ToString()));

        command.ExecuteNonQuery();
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("Во время заполнения таблицы 'main' презентацией '{0}' произошла ошибка: {1}", presInfo.Name, ex.Message));
      }
    }
    #endregion

    /// <summary>
    /// Выложить данные на сервер
    /// </summary>
    /// <param name="presInfo">Информация о презентации</param>
    public override void PutDataOnServer(PresentationInfo presInfo)
    {
      if (presInfo == null)
        throw new ArgumentNullException("информация о презентации должна быть заполнена");

      // Тут важен порядок. Сперва в main, потом в конкретную таблицу, т.к. при добавлении в main определфется Id презентации
      PutDataToMainTable(presInfo);
      PutDataToConcretePresentationTable(presInfo);
    }    
  }
}
