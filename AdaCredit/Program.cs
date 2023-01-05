using System;

using Bogus;

namespace AdaCredit
{
    public class Program
    {
        private static string clientFile = "client_db.csv";

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World");
            GenerateData();
        }

        private static void GenerateData()
        {
            var clientDB = new Data.ClientDB(clientFile);

            var testClients = new Faker<Entities.ClientBase>()
                .RuleFor(m => m.Name, f => f.Name.FullName())
                .RuleFor(m => m.Cpf, f => new Entities.CPF(
                    f.Random.UInt(0, 8) * 111_111_111 +
                    f.Random.UInt(1, 111_111_110)
                ));

            var rnd = new Randomizer();

            // generate 100 accounts
            foreach (var person in testClients.Generate(100))
            {
                var client = clientDB.NewClient(person);
                // generate a random balance
                client.ModifyBalance(Math.Truncate(100 * rnd.Decimal(0, 10000)) / 100);
                // deactivate around 5% of accounts
                if (rnd.Double() < 0.05) { client.Deactivate(); }
            }

            clientDB.Save();


        }
    }
}
