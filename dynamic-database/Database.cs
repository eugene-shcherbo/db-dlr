using System;
using System.Dynamic;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace dynamic_database
{
    public sealed class Database : DynamicObject, IDisposable
    {
        bool _disposed;
        readonly string _connectionString;
        SqlConnection _connection;

        public Database(string connectionString)
        {
            _connectionString = connectionString;
            _disposed = false;
        }

        public void Connect()
        {
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection.Close();
                _disposed = true;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            DataTable schema = _connection.GetSchema("Tables");
            result = new DbTable(_connection, binder.Name);
            return IsThereTableWithName(schema, binder.Name);
        }

        private bool IsThereTableWithName(DataTable schema, string tableName)
        {
            DataColumn nameCol = schema.Columns[2];

            foreach (DataRow row in schema.Rows)
            {
                if ((string)row[nameCol] == tableName)
                {
                    return true;
                }
            }

            return false;
        }

        private class DbTable : DynamicObject
        {
            SqlConnection _conn;
            string _tableName;

            public DbTable(SqlConnection conn, string tableName)
            {
                _conn = conn;
                _tableName = tableName;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                if (IsSearchByQuery(binder.Name))
                {
                    if (IsAsyncQuery(binder.Name))
                    {
                        result = GetSearchByResultsAsync(binder.Name, args);
                    }
                    else
                    {
                        result = GetSearchByResult(binder.Name, args);
                    }

                    return true;
                }

                result = null;
                return false;
            }

            private bool IsSearchByQuery(string query)
            {
                return query.StartsWith("searchby", StringComparison.OrdinalIgnoreCase);
            }

            private bool IsAsyncQuery(string query)
            {
                return query.EndsWith("async", StringComparison.OrdinalIgnoreCase);
            }

            private IEnumerable<dynamic> GetSearchByResult(string searchByQuery, object[] args)
            {
                if (args.Length != 1)
                {
                    throw new ArgumentException("SearchBy should accept only one argument");
                }

                string colName = searchByQuery.Substring("searchby".Length);
                string sql = BuildSql(colName, args[0]);

                SqlDataReader dataReader = PerformSqlAsync(sql)
                    .GetAwaiter()
                    .GetResult();

                return ParseReaderResults(dataReader);
            }

            private Task<IEnumerable<dynamic>> GetSearchByResultsAsync(string searchByQuery, object[] args)
            {
                if (args.Length != 1)
                {
                    throw new ArgumentException("SearchBy should accept only one argument");
                }

                return AsyncImplementation();

                async Task<IEnumerable<dynamic>> AsyncImplementation()
                {
                    int startColName = "searchby".Length;
                    int endColName = searchByQuery.IndexOf("async", StringComparison.OrdinalIgnoreCase);
                    string colName = searchByQuery.Substring(startColName, endColName - startColName);
                    string sql = BuildSql(colName, args[0]);

                    SqlDataReader dataReader = await PerformSqlAsync(sql);

                    return ParseReaderResults(dataReader);
                }
            }

            private string BuildSql(string colName, object val)
            {
                return @$"SELECT * FROM {_tableName} WHERE {colName} = '{val}'";
            }

            private ConfiguredTaskAwaitable<SqlDataReader> PerformSqlAsync(string sql)
            {
                var command = new SqlCommand(sql, _conn);
                return command.ExecuteReaderAsync().ConfigureAwait(false);
            }

            private IEnumerable<dynamic> ParseReaderResults(SqlDataReader reader)
            {
                IEnumerable<DbColumn> columns = reader.GetColumnSchema();
                var items = new List<object>();

                while (reader.Read())
                {
                    var item = new ExpandoObject();
                    foreach (DbColumn col in columns)
                    {
                        item.TryAdd(col.ColumnName, reader[col.ColumnName]);
                    }
                    items.Add(item);
                }

                return items;
            }
        }
    }
}
