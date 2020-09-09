using System;
using System.Dynamic;
using System.Data.SqlClient;

namespace dynamic_database
{
    public class Database : DynamicObject
    {
        private readonly string connectionString;

        public Database(string connectionString)
        {
            this.connectionString = connectionString;
            TestConnection();
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Console.WriteLine("member invoked");
            result = null;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Console.WriteLine("member set");
            return true;
        }

        private void TestConnection()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                Console.WriteLine("Connected successfully.");

                Console.WriteLine("Press any key to finish...");
                Console.ReadKey(true);
            }
        }
    }
}
