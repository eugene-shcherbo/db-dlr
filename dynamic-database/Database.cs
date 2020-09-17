using System;
using System.Dynamic;
using System.Data.SqlClient;
using System.Data;

namespace dynamic_database
{
    // TODO: think how I make it work with any data source easily
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
                // TODO: Create a query parser which will parse query to the query objects (or maybe I can go with expression trees?)
                // TODO: If I'm going to create query objects, think how to represent them (enum constants, classes etc)

                // TODO: How to represent a result of operation?
                // 1. I might have a class for each table
                // 2. I might have a delegate or something to create a result for each table
                // 3. It might be fully dynamic (result is created fron an anonymous type and client will use it as a dynamic variable)

                Console.WriteLine("Request name " + binder.Name);
                result = new { Title = "Hello World" };
                return true;
            }


        }
    }
}
