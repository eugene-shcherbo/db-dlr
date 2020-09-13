using System;
using System.Dynamic;
using System.Data.SqlClient;
using System.Data;

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

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Console.WriteLine("member set");
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            DataTable schema = _connection.GetSchema("Tables");
            result = null; // TODO: create my own table type which inherits from DynamicObject 
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
    }
}
