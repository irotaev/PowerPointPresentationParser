using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerPointPresentation.Lib
{
  /// <summary>
  /// Проверка лицензии
  /// </summary>
  public class LicenseVerifier : IDisposable
  {
    private const string _HostName = "mysql-srv35659.ht-systems.ru";
    private const string _DBName = "srv35659_verifire";
    private const string _UserName = "srv35659_checker";
    private const string _UserPassword = "yt5erw9";

    private readonly MySqlConnection _MySqlConnection;

    public LicenseVerifier()
    {
      try
      {
        _MySqlConnection = new MySqlConnection(String.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};CharSet=utf8", _HostName, _DBName, _UserName, _UserPassword));
        _MySqlConnection.Open();
      }
      catch (Exception ex)
      {
        throw new Exception(String.Format("Ошибка соединения с базой данных проверки лицензии"));
      }
    }

    /// <summary>
    /// Проверить лицензию
    /// </summary>
    /// <returns>True - успешно, false - лицензия блокирована</returns>
    public bool CheckLicense()
    {
      bool result = false;

      try
      {
        MySqlCommand command = _MySqlConnection.CreateCommand();
        command.CommandText = @"SELECT LicenseActive FROM Licenses WHERE ProductCode='AlexandrBilinskiy_PowerPointPresentation'";
        var reader = command.ExecuteReader();

        while(reader.Read())
        {
          Boolean.TryParse(reader.GetString(0), out result);
        }

      }
      catch(Exception ex)
      {
        throw new Exception(String.Format("Не возможно получить данные по лицензии\nПопробуйте повторить операцию позже"));
      }

      return result;
    }
    public void Dispose()
    {
      _MySqlConnection.Close();
    }
  }
}
