using System;
using System.Text;
using Npgsql;
using NATS.Client;
using McMaster.Extensions.CommandLineUtils;

namespace DotNetPgNatsBridge
{
    class MainClass
    {
        public static void Main(string[] _args)
        {
            var app = new CommandLineApplication();

            app.HelpOption();
            var optionPgHost = app.Option("-pgHost|--postgresHost <host>", "Postgres host", CommandOptionType.SingleValue);
            var optionPgPort = app.Option("-pgPort|--postgresPort <port>", "Postgres port", CommandOptionType.SingleValue);
            var optionPgChannel = app.Option("-pgChannel|--postgresChannel <channel>", "Postgres notification channel", CommandOptionType.SingleValue);
            var optionPgDb = app.Option("-pgDB|--postgresDBl <channel>", "Postgres database", CommandOptionType.SingleValue);
            var optionPgUser = app.Option("-pgUser|--postgresUser <user>", "Postgres user", CommandOptionType.SingleValue);
            var optionPgPassword = app.Option("-pgPassword|--postgresPassword <user>", "Postgres password", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var pgHost = optionPgHost.HasValue() ? optionPgHost.Value() : "localhost";
                var pgPort = optionPgPort.HasValue() ? optionPgPort.Value() : "5432";
                var pgChannel = optionPgChannel.HasValue() ? optionPgChannel.Value() : "fixtures";
                var pgDb = optionPgDb.HasValue() ? optionPgDb.Value() : "postgres";
                var pgUser = optionPgUser.HasValue() ? optionPgUser.Value() : "postgres";             // TODO: Implement read from Env first for security
                var pgPassword = optionPgPassword.HasValue() ? optionPgPassword.Value() : "password"; // TODO: Implement read from Env first for security

                // TODO: Replace Console to logging
                Console.WriteLine("");
                Console.WriteLine("Bridging [Db {0}:{1} {2}->{3}] -> NATS", pgHost, pgPort, pgDb, pgChannel);
                Console.WriteLine("");

                Listen(pgHost, pgPort, pgDb, pgUser, pgPassword, pgChannel);

                return 0;
            });

            app.Execute(_args);
        }

        public static void Listen(string host, string port, string database, string username, string password, string channel)
        {
            // Pg Endpoint
            string connectionString = String.Format("Host={0}; Port={1}; Database={2}; Username={3}; Password={4};", host, port, database, username, password);
            var pgConn = new NpgsqlConnection(connectionString);
            pgConn.Open();

            // Nats Endpoint
            Options opts = ConnectionFactory.GetDefaultOptions();
            opts.Url = "localhost:4222";
            IConnection natsConn = new ConnectionFactory().CreateConnection(opts);

            // Setup LISTEN to pg emitted events.
            using (var cmd = new NpgsqlCommand(String.Format("LISTEN {0}", channel), pgConn))
            {
                cmd.ExecuteNonQuery();
            }

            // Configure emit to target endpoint
            pgConn.Notification += (o, e) =>
            {
                Console.WriteLine("{0}: {1}", DateTime.Now.ToString(), e.AdditionalInformation);
                natsConn.Publish("fixtures", Encoding.ASCII.GetBytes(e.AdditionalInformation));
                natsConn.Flush();
            };

            while (true)
            {
                pgConn.Wait();   // Thread will block here
            }
        }
    }
}