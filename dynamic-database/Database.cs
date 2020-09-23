using System;
using System.Dynamic;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;

namespace dynamic_database
{
    // TODO: think how I make it work with any data source easily
    // TODO: graceful error handling
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
            DbQuery _query;

            public DbTable(SqlConnection conn, string tableName)
            {
                _conn = conn;
                _tableName = tableName;
                _query = new DbQuery();
            }

            // TODO: one of the query methods : searchBy, groupBy, between etc should return some new object..

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                // TODO: Create a query parser which will parse query to the query objects (or maybe I can go with expression trees?)
                // TODO: If I'm going to create query objects, think how to represent them (enum constants, classes etc)

                // TODO: How to represent a result of operation?
                // 1. I might have a class for each table
                // 2. I might have a delegate or something to create a result for each table
                // 3. It might be fully dynamic (result is created fron an anonymous type and client will use it as a dynamic variable)

                result = this;

                if (binder.Name.Equals("execute", StringComparison.OrdinalIgnoreCase))
                {
                    SqlDataReader reader = _query.Execute(_tableName, _conn);
                    result = reader;
                    return true;
                }

                if (!DbQueryParser.Parse(binder.Name, args, _query))
                {
                    result = null;
                    return false;
                }

                return true;
            }
        }

        private class DbQueryParser
        {
            static readonly string SearchBy = "searchBy";

            static readonly string QueryNotDefined = null; 

            static ISet<string> _queries = new HashSet<string>
            {
                SearchBy
            };

            public static bool Parse(string queryStr, object[] args, DbQuery query)
            {
                string queryMethod = GetQuery(queryStr);

                if (queryMethod == QueryNotDefined)
                {
                    return false;
                }
                else if (queryMethod == SearchBy)
                {
                    return ParseSearchBy(queryStr, args, query);
                }

                return false;
            }

            static string GetQuery(string queryStr)
            {
                foreach (var query in _queries)
                {
                    if (queryStr.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    {
                        return query;
                    }
                }

                return QueryNotDefined;
            }

            static bool ParseSearchBy(string queryStr, object[] args, DbQuery query)
            {
                if (args.Length != 1)
                {
                    return false;
                }

                string colName = queryStr.Substring(SearchBy.Length);
                query.AddFilterCriteria(colName, args[0].ToString());

                return true;
            }
        }

        private class DbQuery
        {
            private IDictionary<string, string> _filters;

            public DbQuery()
            {
                _filters = new Dictionary<string, string>();
            }

            public void AddFilterCriteria(string columnName, string value)
            {
                _filters.Add(columnName, value);
            }

            public void Reset()
            {
                _filters.Clear();
            }

            public SqlDataReader Execute(string tableName, SqlConnection connection)
            {
                var sql = new SqlCommand(BuildSql(tableName), connection);
                return sql.ExecuteReader();
            }

            private string BuildSql(string tableName)
            {
                var sql = new StringBuilder($"SELECT * FROM {tableName} ");

                if (_filters.Any())
                {
                    sql.Append("WHERE ");

                    foreach (var colName in _filters.Keys)
                    {
                        sql.Append($"{colName} = '{_filters[colName]}' AND ");
                    }

                    // remove AND_ in the end, where _ is space
                    sql.Length -= 4; 
                }

                return sql.ToString();
            }
        }
    }
}
