using System;
using Npgsql;

namespace DotNetPgNatsBridge
{
    class MainClass
    {
        public static void Main(string[] _args)
        {
            Listen("localhost", "5432", "postgres", "postgres", "password", "fixtures");
        }

        public static void Listen(string host, string port, string database, string username, string password, string channel)
        {
            string connectionString = String.Format("Host={0}; Port={1}; Database={2}; Username={3}; Password={4};", host, port, database, username, password);
            var conn = new NpgsqlConnection(connectionString);

            conn.Open();
            conn.Notification += (o, e) => Console.WriteLine("Received notification: {0}, {1}", o, e.AdditionalInformation);

            using (var cmd = new NpgsqlCommand(String.Format("LISTEN {0}", channel), conn))
            {
                cmd.ExecuteNonQuery();
            }

            while (true)
            {
                conn.Wait();   // Thread will block here
            }
        }
    }
}