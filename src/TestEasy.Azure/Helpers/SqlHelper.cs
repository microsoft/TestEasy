using System;
using System.Data;
using System.Data.SqlClient;
using TestEasy.Core;

namespace TestEasy.Azure.Helpers
{
    /// <summary>
    ///     Sql helper api
    /// </summary>
    internal static class SqlHelper
    {
        private const int RetryCount = 5;
        private const int DefaultCommandTimeout = 120; // secopnds

        /// <summary>
        ///     Safe execute sql query (no exceptions)
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="sql"></param>
        public static void ExecuteQuery(string connectionString, string sql)
        {
            SafeExecuteQuery(() =>
                {
                    var connection = new SqlConnection(connectionString);
                    var command = new SqlCommand(sql, connection) {CommandTimeout = DefaultCommandTimeout};

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        TestEasyLog.Instance.Warning(
                            string.Format(
                                "There was some exception while executing sql query: '{0}', Stack trace: '{1}', connectionString: '{2}', sqlQuery:'{3}'",
                                e.Message, e.StackTrace, connectionString, sql));
                        throw;
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }

                    return null;
                });
        }

        private static object SafeExecuteQuery(Func<object>  action)
        {
            object result = null;
            var success = false;
            var count = 0;
            while (!success && count < RetryCount)
            {
                try
                {
                    result = action.Invoke();
                    success = true;
                }
                catch (Exception e)
                {
                    TestEasyLog.Instance.Warning(
                        string.Format("There was some exception while executing sql query: '{0}', Stack trace: '{1}'",
                                      e.Message, e.StackTrace));
                }

                count++;
            }

            if (!success)
            {
                throw new Exception(string.Format("There was some problem while running SQL query after {0} retries. See exceptions in the log.", RetryCount));
            }

            return result;
        }

        /// <summary>
        ///     Safe execute select query
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static DataTable ExecuteSelectQuery(string connectionString, string sql)
        {
            return (DataTable)SafeExecuteQuery(() =>
                {

                    var table = new DataTable();
                    var connection = new SqlConnection(connectionString);
                    var command = new SqlCommand(sql, connection) {CommandTimeout = DefaultCommandTimeout};

                    try
                    {
                        connection.Open();
                        table.Load(command.ExecuteReader());
                    }
                    catch (Exception e)
                    {
                        TestEasyLog.Instance.Warning(
                            string.Format(
                                "There was some exception while executing sql query: '{0}', Stack trace: '{1}', connectionString: '{2}', sqlQuery:'{3}'",
                                e.Message, e.StackTrace, connectionString, sql));
                        throw;
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }

                    return table;
                });
        }

        /// <summary>
        ///     Safe execute function query
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string connectionString, string sql)
        {
            return SafeExecuteQuery(() =>
                {
                    object result = null;
                    var connection = new SqlConnection(connectionString);
                    var command = new SqlCommand(sql, connection) {CommandTimeout = DefaultCommandTimeout};

                    try
                    {
                        connection.Open();
                        result = command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        TestEasyLog.Instance.Warning(
                            string.Format(
                                "There was some exception while executing sql query: '{0}', Stack trace: '{1}', connectionString: '{2}', sqlQuery:'{3}'",
                                e.Message, e.StackTrace, connectionString, sql));
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                    return result;
                });
        }        
    }
}
