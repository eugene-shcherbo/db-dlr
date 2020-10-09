using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace dynamic_database
{
    class Program
    {
        static async Task Main(string[] args)
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

            try
            {
                await db.Books.SearchByAuthorAsync("Jon Skeet", "Hello World");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }

            dynamic books = await db.Books.SearchByAuthorAsync("Jon Skeet");

            foreach (var book in books)
            {
                Console.WriteLine(book.title);
            }
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
