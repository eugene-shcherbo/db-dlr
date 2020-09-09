using System;
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

            dynamic database = new Database(connectioString);
            Action helloWorld = () => { Console.WriteLine("Hello World"); };
            database.HelloWorld = helloWorld;
            database.HelloWorld();
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
