using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerPointPresentation
{
  public interface IAbstractDBTable
  {
    /// <summary>
    /// Выложить данные на сервер
    /// </summary>
    /// <param name="presInfo">Информация о презентации</param>
    void PutDataOnServer(PresentationInfo presInfo);
  }

  public interface ILastInsertedPPTInfoId
  {
    /// <summary>
    /// Получить id последней строки с информацией о презентации в базе
    /// </summary>
    /// <returns>id строки</returns>
    long GetCurrentPresentationIndex();
  }

  public abstract class AbstractDBTable : IDisposable, IAbstractDBTable, ILastInsertedPPTInfoId
  {
    /// <summary>
    /// Создать объект таблицы презентации
    /// </summary>
    /// <param name="hostName">Имя хоста</param>
    /// <param name="dbName">Имя базы данных</param>
    /// <param name="userName">Имя пользователя</param>
    /// <param name="userPassword">Пароль пользователя</param>
    public AbstractDBTable(string hostName, string dbName, string userName, string userPassword, string tableName)
    {
      try
      {
        _MainTableName = tableName;

        _MySqlConnection = new MySqlConnection(String.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};CharSet=utf8", hostName, dbName, userName, userPassword));
        _MySqlConnection.Open();
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("Ошибка соединения с базой данных: {0}", ex.Message));
      }
    }

    #region Properties
    public const string TABLE_CHARSET = "utf8mb4";
    protected readonly MySqlConnection _MySqlConnection;
    protected readonly string _MainTableName;
    #endregion

    #region Methods
    public void Dispose()
    {
      _MySqlConnection.Close();
    }

    public abstract void PutDataOnServer(PresentationInfo presInfo);

    public abstract long GetCurrentPresentationIndex();
    #endregion
  }
}
