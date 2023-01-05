using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using Bogus;

namespace AdaCredit
{
    public class Program
    {
        private static uint bankCode = 777;

        private static Data.ClientDB clientDB = new Data.ClientDB("client_db.csv");

        private static string pendingDir
            = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Transactions", "Pending"
            );
        private static string completedDir
            = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Transactions", "Completed"
             );
        private static string failedDir
            = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Transactions", "Failed"
            );

        private static string transactionPrefix = "ada-credit-";

        private static string pendingSuffix = ".csv";
        private static string completedSuffix = "-completed.csv";
        private static string failedSuffix = "-failed.csv";

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World");

            // make sure all necessary directories exist
            if (!Directory.Exists(pendingDir))
            {
                Directory.CreateDirectory(pendingDir);
            }
            if (!Directory.Exists(completedDir))
            {
                Directory.CreateDirectory(completedDir);
            }
            if (!Directory.Exists(failedDir))
            {
                Directory.CreateDirectory(failedDir);
            }

            GenerateData(100, 250);
        }

        private static void GenerateData(int nClients, int nTransactions)
        {
            var testClients = new Faker<Entities.ClientBase>()
                .RuleFor(m => m.Name, f => f.Name.FullName())
                .RuleFor(m => m.Cpf, f => new Entities.CPF(
                    f.Random.UInt(0, 8) * 111_111_111 +
                    f.Random.UInt(1, 111_111_110)
                ));

            var rnd = new Randomizer();

            // generate accounts
            clientDB.Clear();
            foreach (var person in testClients.Generate(nClients))
            {
                var client = clientDB.NewClient(person);
                // generate a random balance
                client.ModifyBalance(Math.Truncate(100 * rnd.Decimal(0, 10000)) / 100);
                // deactivate around 5% of accounts
                if (rnd.Double() < 0.05) { client.Deactivate(); }
            }
            clientDB.Save();

            // generate another 10% fake account numbers
            var accounts = clientDB.Keys.ToList();
            for (int i = 0; i < nClients / 10; i++)
            {
                accounts.Add((1, rnd.UInt(0, 999_999)));
            }

            var transactions = new List<Entities.Transaction>();

            // some transactions within our bank
            var testTransactions = new Faker<Entities.Transaction>()
                .RuleFor(m => m.sBank, f => bankCode)
                .RuleFor(m => m.sBranch, f => (uint)1)
                .RuleFor(m => m.sAccount, f => f.Random.ListItem(accounts).Item2)
                .RuleFor(m => m.tBank, f => bankCode)
                .RuleFor(m => m.tBranch, f => (uint)1)
                .RuleFor(m => m.tAccount, f => f.Random.ListItem(accounts).Item2)
                .RuleFor(m => m.Type, f => f.Random.Enum<Entities.TransactionType>())
                .RuleFor(m => m.Amount, f => Math.Truncate(100 * f.Random.Decimal(0, 1000)) / 100);
            transactions.AddRange(testTransactions.Generate(nTransactions * 3 / 10));

            // some where we're the source
            testTransactions = new Faker<Entities.Transaction>()
                .RuleFor(m => m.sBank, f => bankCode)
                .RuleFor(m => m.sBranch, f => (uint)1)
                .RuleFor(m => m.sAccount, f => f.Random.ListItem(accounts).Item2)
                .RuleFor(m => m.tBank, f => f.Random.UInt(0, 999))
                .RuleFor(m => m.tBranch, f => f.Random.UInt(0, 9999))
                .RuleFor(m => m.tAccount, f => f.Random.UInt(0, 999_999))
                .RuleFor(m => m.Type, f => f.Random.Enum<Entities.TransactionType>())
                .RuleFor(m => m.Amount, f => Math.Truncate(100 * f.Random.Decimal(0, 1000)) / 100);
            transactions.AddRange(testTransactions.Generate(nTransactions * 3 / 10));

            // some where we're the target
            testTransactions = new Faker<Entities.Transaction>()
                .RuleFor(m => m.sBank, f => f.Random.UInt(0, 999))
                .RuleFor(m => m.sBranch, f => f.Random.UInt(0, 9999))
                .RuleFor(m => m.sAccount, f => f.Random.UInt(0, 999_999))
                .RuleFor(m => m.tBank, f => bankCode)
                .RuleFor(m => m.tBranch, f => (uint)1)
                .RuleFor(m => m.tAccount, f => f.Random.ListItem(accounts).Item2)
                .RuleFor(m => m.Type, f => f.Random.Enum<Entities.TransactionType>())
                .RuleFor(m => m.Amount, f => Math.Truncate(100 * f.Random.Decimal(0, 1000)) / 100);
            transactions.AddRange(testTransactions.Generate(nTransactions * 3 / 10));

            // and finally some completely random ones
            testTransactions = new Faker<Entities.Transaction>()
                .RuleFor(m => m.sBank, f => f.Random.UInt(0, 999))
                .RuleFor(m => m.sBranch, f => f.Random.UInt(0, 9999))
                .RuleFor(m => m.sAccount, f => f.Random.UInt(0, 999_999))
                .RuleFor(m => m.tBank, f => f.Random.UInt(0, 999))
                .RuleFor(m => m.tBranch, f => f.Random.UInt(0, 9999))
                .RuleFor(m => m.tAccount, f => f.Random.UInt(0, 999_999))
                .RuleFor(m => m.Type, f => f.Random.Enum<Entities.TransactionType>())
                .RuleFor(m => m.Amount, f => Math.Truncate(100 * f.Random.Decimal(0, 1000) / 100));
            transactions.AddRange(testTransactions.Generate(nTransactions - transactions.Count));

            // now we randomly spread the transactions between 2022-11-26 and 2022-12-05
            var dateStart = new DateOnly(2022, 11, 26);
            int nDays = 10;
            var dailyTransactions = new List<List<Entities.Transaction>>();
            for (int i = 0; i < nDays; i++)
            {
                dailyTransactions.Add(new List<Entities.Transaction>());
            }

            foreach (var transaction in rnd.Shuffle(transactions))
            {
                dailyTransactions[rnd.Int(0, nDays - 1)].Add(transaction);
            }

            // and we want to save these to files
            // delete any old transaction data
            // to get a clean slate
            foreach (var file in new DirectoryInfo(pendingDir).EnumerateFiles()) { file.Delete(); }
            foreach (var file in new DirectoryInfo(completedDir).EnumerateFiles()) { file.Delete(); }
            foreach (var file in new DirectoryInfo(failedDir).EnumerateFiles()) { file.Delete(); }

            // and save
            for (int i = 0; i < nDays; i++)
            {
                var date = dateStart.AddDays(i);
                string filename = Path.Join(
                    pendingDir,
                    transactionPrefix +
                    date.ToString("yyyyMMdd") +
                    pendingSuffix
                );
                using (var reader = new StreamWriter(filename))
                using (var csv = new CsvHelper.CsvWriter(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<Entities.TransactionMap>();
                    foreach (var record in dailyTransactions[i])
                    {
                        csv.WriteRecord(record);
                        csv.NextRecord();
                    }
                }
            }
        }
    }
}
