using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace ContextDbSP
{
    public class ContextDb : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly SqlConnection _connection;
        private readonly SqlCommand _command;
        private string _parameters = string.Empty;

        public ContextDb(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            try
            {
                _connection = new SqlConnection(connectionString);
                _command = new SqlCommand { CommandType = CommandType.StoredProcedure };
                _connection.Open();
                Logger.Info($"Connected to SQL Server: {builder.DataSource}, Databse: {builder.InitialCatalog}");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed connect to SQL Server: {builder.DataSource}, Databse: {builder.InitialCatalog}", e, e.Message);
                throw new Exception(e.Message);
            }
        }

        public void SetCommandText<T>(T data)
        {
            _command.CommandText = typeof(T).Name;

            Logger.Info($"Init stored procedure: {_command.CommandText}");
            _parameters = string.Empty;
            _command.Parameters.Clear();
            foreach (var property in typeof(T).GetProperties())
            {
                var key = property.Name;
                var propInfo = data.GetType().GetProperty(key);
                if (propInfo == null) continue;
                var val = propInfo.GetValue(data, null);
                var param = new SqlParameter()
                {
                    Value = val,
                    ParameterName = key
                };
                _command.Parameters.Add(param);
                _parameters += $"{key}:{val}, ";
                Logger.Info($"Added param: {key}, value: {val} ");
            }
        }

        public IEnumerable<T> ExecuteToList<T>(string storedProcedureName = null)
        {
            return Execute<T>(storedProcedureName);
        }

        public T ExecuteFirstOrDefault<T>(string storedProcedureName = null)
        {
            return Execute<T>(storedProcedureName).FirstOrDefault();
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }

        private IEnumerable<T> Execute<T>(string storedProcedureName = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (storedProcedureName != null)
            {
                _command.CommandText = storedProcedureName;
                _command.Parameters.Clear();
            }

            _command.Connection = _connection;
            SqlDataReader reader;
            try
            {
                reader = _command.ExecuteReader();
                Logger.Info($"Execute stored procedure: {_command.CommandText}");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed execute stored procedure: {_command.CommandText}", e, e.Message);
                throw new Exception(e.Message);
            }

            var timeSql = sw.Elapsed;

            var resultList = (List<T>)Activator.CreateInstance(typeof(List<T>));

            if (reader.HasRows)
            {
                var fields = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                var isCohesionModels = true;
                while (reader.Read())
                {
                    var resultItem = (T)Activator.CreateInstance(typeof(T));
                    foreach (var prop in typeof(T).GetProperties())
                    {
                        if (fields.Contains(prop.Name))
                        {
                            var val = reader[prop.Name];
                            var key = prop.Name;
                            try
                            {
                                resultItem.GetType().GetProperty(key)?.SetValue(resultItem, val is DBNull ? null : val, null);
                            }
                            catch (Exception e)
                            {
                                reader.Close();
                                Logger.Error($"Structure error SQL <==> CLASS: {e.Message}", e, e.Message);
                                throw new Exception($"Structure error stored procedure [{_command.CommandText}] SQL <==> CLASS. {e.Message}");
                            }
                        }
                        else
                        {
                            isCohesionModels = false;
                        }
                    }
                    resultList.Add(resultItem);
                }

                if (isCohesionModels == false)
                    Logger.Warn($"There was an inconsistency in the data structure SQL <==> CLASS:");

            }

            reader.Close();
            sw.Stop();
            Logger.Trace($"Performance time: all:{sw.Elapsed.Milliseconds}ms, sql:{timeSql.Milliseconds}ms, stored procedure name:{_command.CommandText}, parameters: {_parameters}");
            return resultList;
        }

    }
}

