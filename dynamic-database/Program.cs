using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace dynamic_database
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfigurationRoot config = CreateConfig();
            var connectioString = config.GetValue<string>("ConnectionString");

            if (string.IsNullOrWhiteSpace(connectioString))
            {
                Console.WriteLine("Please provide a connection string");
                return;
            }

            using dynamic db = new Database(connectioString);
            db.Connect();

            dynamic res = db.Books
                .SearchByPrice(20)
                .SearchByTitle("C# in Depth")
                .Execute();

            Console.WriteLine(res.ToString());
        }

        static IConfigurationRoot CreateConfig()
        {
            var builder = new ConfigurationBuilder();
            BuildConfig(builder);
            return builder.Build();
        }

        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>();
        }
    }
}
