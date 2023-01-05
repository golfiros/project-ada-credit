using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using Bogus;

using CsvHelper;
using CsvHelper.Configuration;

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
            Console.WriteLine("Sistema bancário Ada Credit");

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

            clientDB.Load();
        }

        private void AddClient()
        {
            // there's gotta be a better way to do this
            // using reflection instead of just *knowing*
            // the properties of ClientBase, but I couldn't
            // figure it out

            string? name;
            Entities.CPF cpf;

            Console.Write("Nome do cliente: ");
            while ((name = Console.ReadLine()) is null || name == "")
            {
                Console.WriteLine("Entrada inválida: ");
            }
            Console.Write("CPF do cliente: ");
            while (!Entities.CPF.TryParse(Console.ReadLine(), out cpf))
            {
                Console.Write("Entrada inválida: ");
            }
            var client = clientDB.NewClient(new Entities.ClientBase { Name = name, Cpf = cpf });
            Console.WriteLine($"Criado cliente de ag. {client.Branch:0000} e c.c. {client.Account:00000-0}");
        }

        private void PrintClient()
        {
            uint account;
            Console.Write("Número da conta: ");
            string? input;
            while (
                (input = Console.ReadLine()) is null ||
                !uint.TryParse(string.Concat(input.Where(Char.IsDigit).ToArray()), out account) ||
                account > 999_999)
            {
                Console.Write("Entrada inválida: ");
            }
            var client = clientDB.GetClient(1, account);
            if (client is null)
            {
                Console.WriteLine("Cliente não encontrado");
                return;
            }
            Console.WriteLine(client);
        }

        private void EditClient()
        {
            uint account;
            Console.Write("Número da conta: ");
            string? input;
            while (
                (input = Console.ReadLine()) is null ||
                !uint.TryParse(string.Concat(input.Where(Char.IsDigit).ToArray()), out account) ||
                account > 999_999)
            {
                Console.Write("Entrada inválida: ");
            }
            var client = clientDB.GetClient(1, account);
            if (client is null)
            {
                Console.WriteLine("Cliente não encontrado");
                return;
            }
            // again there must be a better way to do
            // this with reflection
        }

        private void DeactivateClient()
        {
            uint account;
            Console.Write("Número da conta: ");
            string? input;
            while (
                (input = Console.ReadLine()) is null ||
                !uint.TryParse(string.Concat(input.Where(Char.IsDigit).ToArray()), out account) ||
                account > 999_999)
            {
                Console.Write("Entrada inválida: ");
            }
            var client = clientDB.GetClient(1, account);
            if (client is null)
            {
                Console.WriteLine("Cliente não encontrado");
                return;
            }
            client.Deactivate();
            Console.WriteLine("Cliente desativado com sucesso");
        }

        private void AddUser()
        {
            string? user, pass, confirm;
            Console.Write("Nome de usuário: ");
            while ((user = Console.ReadLine()) is null || user == "")
            {
                Console.WriteLine("Entrada inválida: ");
            }

            Console.Write("Senha: ");
            while ((pass = Console.ReadLine()) is null)
            {
                Console.WriteLine("Entrada inválida: ");
            }

            Console.Write("Confirme a senha: ");
            while ((confirm = Console.ReadLine()) is null && confirm != pass)
            {
                Console.WriteLine("Entrada inválida: ");
            }

            Console.WriteLine("Usuário cadastrado com sucesso");
        }

        private static void ProcessTransactions()
        {
            foreach (var file in new DirectoryInfo(pendingDir).EnumerateFiles())
            {
                // make sure we're looking at a decent filename
                DateOnly date;
                if (!DateOnly.TryParseExact(
                    file.Name.Replace(transactionPrefix, "").Replace(pendingSuffix, ""),
                    "yyyyMMdd",
                    out date
                )) { continue; }

                List<Entities.Transaction> pending;
                using (var reader = new StreamReader(file.FullName))
                using (var csv = new CsvReader(
                    reader,
                    new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false
                    }
                ))
                {
                    csv.Context.RegisterClassMap<Entities.TransactionMap>();
                    pending = csv.GetRecords<Entities.Transaction>().ToList();
                }
                var completed = new List<Entities.Transaction>();
                var failed = new List<(Entities.Transaction, Entities.TransactionResult)>();

                foreach (var transaction in pending)
                {
                    Entities.Client? source, target;
                    if (transaction.sBank != bankCode && transaction.tBank != bankCode)
                    {
                        completed.Add(transaction);
                        continue;
                    }
                    if (transaction.sBank != bankCode)
                    {
                        target = clientDB.GetClient(transaction.tBranch, transaction.tAccount);
                        if (target is null || !target.IsActive)
                        {
                            failed.Add((transaction, Entities.TransactionResult.INVALID_TARGET));
                            continue;
                        }
                        if (transaction.Type == Entities.TransactionType.TEF)
                        {
                            failed.Add((transaction, Entities.TransactionResult.INVALID_TYPE));
                            continue;
                        }
                        target.ModifyBalance(transaction.Amount);
                        completed.Add(transaction);
                        continue;
                    }
                    if (transaction.tBank != bankCode)
                    {
                        source = clientDB.GetClient(transaction.sBranch, transaction.sAccount);
                        if (source is null || !source.IsActive)
                        {
                            failed.Add((transaction, Entities.TransactionResult.INVALID_SOURCE));
                            continue;
                        }
                        if (transaction.Type == Entities.TransactionType.TEF)
                        {
                            failed.Add((transaction, Entities.TransactionResult.INVALID_TYPE));
                            continue;
                        }
                        if (!source.ModifyBalance(-(transaction.Amount + transaction.Tariff(date))))
                        {
                            failed.Add((transaction, Entities.TransactionResult.INSUFFICIENT_BALANCE));
                            continue;
                        }
                        completed.Add(transaction);
                        continue;
                    }
                    source = clientDB.GetClient(transaction.sBranch, transaction.sAccount);
                    if (source is null || !source.IsActive)
                    {
                        failed.Add((transaction, Entities.TransactionResult.INVALID_SOURCE));
                        continue;
                    }
                    target = clientDB.GetClient(transaction.tBranch, transaction.tAccount);
                    if (target is null || !target.IsActive)
                    {
                        failed.Add((transaction, Entities.TransactionResult.INVALID_TARGET));
                        continue;
                    }
                    if (!source.ModifyBalance(-(transaction.Amount + transaction.Tariff(date))))
                    {
                        failed.Add((transaction, Entities.TransactionResult.INSUFFICIENT_BALANCE));
                        continue;
                    }
                    target.ModifyBalance(transaction.Amount);
                    completed.Add(transaction);
                    continue;
                }

                // now we write the completed and failed transactions to files
                string completedFile = Path.Join(
                    completedDir,
                    transactionPrefix +
                    date.ToString("yyyyMMdd") +
                    completedSuffix
                );
                using (var writer = new StreamWriter(completedFile))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<Entities.TransactionMap>();
                    foreach (var record in completed)
                    {
                        csv.WriteRecord(record);
                        csv.NextRecord();
                    }
                }

                string failedFile = Path.Join(
                    failedDir,
                    transactionPrefix +
                    date.ToString("yyyyMMdd") +
                    failedSuffix
                );
                using (var writer = new StreamWriter(failedFile))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<Entities.TransactionMap>();
                    foreach (var record in failed)
                    {
                        csv.WriteRecord(record.Item1);
                        csv.NextRecord();
                        var resultRecord = new { record = record.Item2 };
                        csv.WriteRecord(resultRecord);
                        csv.NextRecord();
                    }
                }

                // delete the original transaction file
                file.Delete();
                // save the client database
                clientDB.Save();
            }
        }

        private static void PrintActiveClients()
        {
            foreach (var client in clientDB.Clients.Where(c => c.IsActive))
            {
                Console.WriteLine(client);
            }
        }

        private static void PrintInactiveClients()
        {
            foreach (var client in clientDB.Clients.Where(c => c.IsActive))
            {
                Console.WriteLine(client);
            }
        }

        private static void PrintActiveUsers() { }

        private static void PrintFailures()
        {
            foreach (var file in new DirectoryInfo(failedDir).EnumerateFiles())
            {
                DateOnly date;
                if (!DateOnly.TryParseExact(
                    file.Name.Replace(transactionPrefix, "").Replace(failedSuffix, ""),
                    "yyyyMMdd",
                    out date
                )) { continue; }
                Console.WriteLine(date);
                using (var reader = new StreamReader(file.FullName))
                using (var csv = new CsvReader(
                    reader,
                    new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false
                    }
                ))
                {
                    csv.Context.RegisterClassMap<Entities.TransactionMap>();
                    var resultRecord = new { record = default(Entities.TransactionResult) };
                    while (csv.Read())
                    {
                        Console.WriteLine(csv.GetRecord<Entities.Transaction>());
                        csv.Read();
                        resultRecord = csv.GetRecord(resultRecord);
                        if (resultRecord is null)
                        {
                            Console.WriteLine("Erro desconhecido");
                            continue;
                        }
                        Console.WriteLine(
                            resultRecord.record switch
                            {
                                Entities.TransactionResult.INVALID_SOURCE => "Conta de origem inválida",
                                Entities.TransactionResult.INVALID_TARGET => "Conta de destino inválida",
                                Entities.TransactionResult.INVALID_TYPE => "Tipo de transação inválido",
                                Entities.TransactionResult.INSUFFICIENT_BALANCE => "Saldo insuficiente",
                                _ => "Erro desconhecido"
                            }
                        );
                    }
                }
                Console.WriteLine();
            }
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
                client.ModifyBalance(Math.Truncate(100 * rnd.Decimal(0, 5000)) / 100);
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
                using (var csv = new CsvWriter(reader, CultureInfo.InvariantCulture))
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
