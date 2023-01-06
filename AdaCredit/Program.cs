using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using Bogus;

using CsvHelper;
using CsvHelper.Configuration;

using ConsoleTools;

namespace AdaCredit
{
    public class Program
    {
        private static uint bankCode = 777;

        private static Data.ClientDB clientDB = new Data.ClientDB("client_db.csv");
        private static Data.UserDB userDB = new Data.UserDB("user_db.csv");

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

            userDB.Load();

            Entities.User currUser = Login();

            currUser.LastLogin = DateTime.Now;

            userDB.Save();

            clientDB.Load();

            var clientMenu = new ConsoleMenu(args, level: 1)
                .Add("Cadastrar novo cliente", AddClient)
                .Add("Consultar dados de cliente existente", PrintClient)
                .Add("Alterar o cadastro de cliente existente", EditClient)
                .Add("Desativar cadastro de cliente existente", DeactivateClient)
                .Add("Voltar", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.Title = "Clientes";
                    config.EnableWriteTitle = true;
                    config.EnableBreadcrumb = false;
                    config.WriteHeaderAction = () => { };
                });

            var userMenu = new ConsoleMenu(args, level: 1)
                .Add("Cadastrar novo funcionário", () => AddUser())
                .Add("Alterar senha de funcionário existente", EditUser)
                .Add("Desativar cadastro de funcionário existente", DeactivateUser)
                .Add("Voltar", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.Title = "Funcionários";
                    config.EnableWriteTitle = true;
                    config.EnableBreadcrumb = false;
                    config.WriteHeaderAction = () => { };
                });

            var dataMenu = new ConsoleMenu(args, level: 1)
                .Add("Exibir clientes ativos", PrintActiveClients)
                .Add("Exibir clientes inativos", PrintInactiveClients)
                .Add("Exibir funcionários ativos", PrintActiveUsers)
                .Add("Exibir transações com erro", PrintFailures)
                .Add("Voltar", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.Title = "Relatórios";
                    config.EnableWriteTitle = true;
                    config.EnableBreadcrumb = false;
                    config.WriteHeaderAction = () => { };
                });

            var genMenu = new ConsoleMenu(args, level: 1)
                .Add("Não", ConsoleMenu.Close)
                .Add("Sim", (m) => { GenerateData(100, 300); m.CloseMenu(); })
                .Configure(config =>
                {
                    config.Title = "Gerar novos dados vai apagar todos os dados existentes. Deseja continuar?";
                    config.EnableWriteTitle = true;
                    config.EnableBreadcrumb = false;
                    config.WriteHeaderAction = () => { };
                });

            var mainMenu = new ConsoleMenu(args, level: 0)
                .Add("Clientes", clientMenu.Show)
                .Add("Funcionários", userMenu.Show)
                .Add("Processar Transações", ProcessTransactions)
                .Add("Relatórios", dataMenu.Show)
                .Add("Gerar Dados", genMenu.Show)
                .Add("Sair", () => Environment.Exit(0))
                .Configure(config =>
                {
                    config.Title = $"Bem vindo {currUser.Username}, escolha uma opção:";
                    config.EnableWriteTitle = true;
                    config.EnableBreadcrumb = false;
                    config.WriteHeaderAction = () => { };
                });

            mainMenu.Show();
        }

        private static Entities.User Login()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Sistema Ada Credit");
                Console.Write("Nome de usuário: ");
                string? name;
                while ((name = Console.ReadLine()) is null) ;

                Console.Write("Senha: ");
                string pass = "";
                ConsoleKey key;
                do
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && pass.Length > 0) { pass = pass[0..^1]; }
                    else if (!char.IsControl(keyInfo.KeyChar)) { pass += keyInfo.KeyChar; }
                } while (key != ConsoleKey.Enter);
                Console.WriteLine();
                if (!userDB.Users.Any() && name == "user" && pass == "pass")
                {
                    Console.WriteLine("Primeiro login, por favor crie um novo usuário");
                    System.Threading.Thread.Sleep(2000);
                    return AddUser();
                }
                Entities.User? user;
                if ((user = userDB.GetUser(name)) is not null && user.CheckPass(pass) && user.IsActive)
                {
                    return user;
                }
                Console.WriteLine("Credenciais inválidas, tente novamente");
                System.Threading.Thread.Sleep(2000);
            }
        }

        private static void AddClient()
        {
            // there's gotta be a better way to do this
            // using reflection instead of just *knowing*
            // the properties of ClientBase, but I couldn't
            // figure it out

            // that's mostly the reason why I have so few
            // personal info properties in the first place

            Console.Clear();
            Console.WriteLine("Cadastro de cliente");

            string? name;
            Entities.CPF cpf;

            Console.Write("Nome do cliente: ");
            while ((name = Console.ReadLine()) is null || name == "")
            {
                Console.Write("Entrada inválida: ");
            }
            Console.Write("CPF do cliente: ");
            while (!Entities.CPF.TryParse(Console.ReadLine(), out cpf))
            {
                Console.Write("Entrada inválida: ");
            }
            var client = clientDB.NewClient(new Entities.ClientBase { Name = name, Cpf = cpf });
            clientDB.Save();
            Console.WriteLine($"Criado cliente de ag. {client.Branch:0000} e c.c. {client.Account:00000-0}");
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void PrintClient()
        {
            Console.Clear();
            Console.WriteLine("Consulta de cliente");

            uint account;
            Console.Write("Número da conta: ");
            string? input;
            Entities.Client? client = null;
            while (
                (input = Console.ReadLine()) is null ||
                !uint.TryParse(string.Concat(input.Where(Char.IsDigit).ToArray()), out account) ||
                account > 999_999 ||
                (client = clientDB.GetClient(1, account)) is null)
            {
                Console.Write("Entrada inválida: ");
            }
            Console.WriteLine(client);
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void EditClient()
        {
            Console.Clear();
            Console.WriteLine("Alteração de cliente");

            uint account;
            Console.Write("Número da conta: ");
            string? input;
            Entities.Client? client = null;
            while (
                (input = Console.ReadLine()) is null ||
                !uint.TryParse(string.Concat(input.Where(Char.IsDigit).ToArray()), out account) ||
                account > 999_999 ||
                (client = clientDB.GetClient(1, account)) is null)
            {
                Console.Write("Entrada inválida: ");
            }
            // again there must be a better way to do
            // this with reflection

            new ConsoleMenu()
                .Add("Nome", (m) =>
                {
                    Console.Clear();
                    string? name;
                    Console.Write("Novo nome do cliente: ");
                    while ((name = Console.ReadLine()) is null || name == "")
                    {
                        Console.WriteLine("Entrada inválida: ");
                    }
                    client.Name = name;
                    m.CloseMenu();
                })
                .Add("CPF", (m) =>
                {
                    Console.Clear();
                    Entities.CPF cpf;
                    Console.Write("Novo CPF do cliente: ");
                    while (!Entities.CPF.TryParse(Console.ReadLine(), out cpf))
                    {
                        Console.Write("Entrada inválida: ");
                    }
                    client.Cpf = cpf;
                    m.CloseMenu();
                })
                .Configure(config =>
                {
                    config.Title = "Selecione um dado para alterar";
                    config.EnableWriteTitle = true;
                    config.EnableBreadcrumb = false;
                    config.WriteHeaderAction = () => { };
                })
                .Show();
            clientDB.Save();
            Console.WriteLine("Cadastro alterado com sucesso");
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void DeactivateClient()
        {
            Console.Clear();
            Console.WriteLine("Desativar cliente");

            uint account;
            Console.Write("Número da conta: ");
            string? input;
            Entities.Client? client = null;
            while (
                (input = Console.ReadLine()) is null ||
                !uint.TryParse(string.Concat(input.Where(Char.IsDigit).ToArray()), out account) ||
                account > 999_999 ||
                (client = clientDB.GetClient(1, account)) is null)
            {
                Console.Write("Entrada inválida: ");
            }
            client.Deactivate();
            clientDB.Save();
            Console.WriteLine("Cliente desativado com sucesso");
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static Entities.User AddUser()
        {
            Console.Clear();
            Console.WriteLine("Cadastro de usuário");

            string? name;
            Entities.User? user = null;
            Console.Write("Nome de usuário: ");
            while ((name = Console.ReadLine()) is null ||
                    name == "" ||
                    (user = userDB.GetUser(name)) is not null)
            {
                if (user is not null)
                {
                    Console.WriteLine("Usuário já cadastrado");
                }
                Console.Write("Entrada inválida: ");
            }

            string pass, confirm;
            while (true)
            {
                ConsoleKey key;

                Console.Write("Senha: ");
                pass = "";
                do
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && pass.Length > 0) { pass = pass[0..^1]; }
                    else if (!char.IsControl(keyInfo.KeyChar)) { pass += keyInfo.KeyChar; }
                } while (key != ConsoleKey.Enter);
                Console.WriteLine();
                Console.Write("Confirme a senha: ");

                confirm = "";
                do
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && confirm.Length > 0) { confirm = confirm[0..^1]; }
                    else if (!char.IsControl(keyInfo.KeyChar)) { confirm += keyInfo.KeyChar; }
                } while (key != ConsoleKey.Enter);
                Console.WriteLine();
                if (confirm == pass) { break; }
                {
                    Console.WriteLine("Senhas diferentes, tente novamente");
                }
            }

            user = userDB.NewUser(name);
            if (user is null) { throw new NullReferenceException(); }
            user.ChangePass(pass);

            userDB.Save();

            Console.WriteLine("Usuário cadastrado com sucesso");
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();

            return user;
        }

        private static void EditUser()
        {
            Console.Clear();
            Console.WriteLine("Alteração de senha");

            string? name;
            Entities.User? user = null;
            Console.Write("Nome de usuário: ");
            while ((name = Console.ReadLine()) is null ||
                    name == "" ||
                    (user = userDB.GetUser(name)) is null)
            {
                Console.WriteLine("Entrada inválida: ");
            }

            string pass, confirm;
            while (true)
            {
                ConsoleKey key;

                Console.Write("Senha: ");
                pass = "";
                do
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && pass.Length > 0) { pass = pass[0..^1]; }
                    else if (!char.IsControl(keyInfo.KeyChar)) { pass += keyInfo.KeyChar; }
                } while (key != ConsoleKey.Enter);
                Console.WriteLine();
                Console.Write("Confirme a senha: ");

                confirm = "";
                do
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && confirm.Length > 0) { confirm = confirm[0..^1]; }
                    else if (!char.IsControl(keyInfo.KeyChar)) { confirm += keyInfo.KeyChar; }
                } while (key != ConsoleKey.Enter);
                Console.WriteLine();
                if (confirm == pass) { break; }
                {
                    Console.WriteLine("Senhas diferentes, tente novamente");
                }
            }

            user.ChangePass(pass);

            userDB.Save();

            Console.WriteLine("Senha alterada com sucesso");
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void DeactivateUser()
        {
            Console.Clear();
            Console.WriteLine("Desativar funcionário");

            string? name;
            Entities.User? user = null;
            Console.Write("Nome de usuário: ");
            while ((name = Console.ReadLine()) is null ||
                    name == "" ||
                    (user = userDB.GetUser(name)) is null)
            {
                Console.WriteLine("Entrada inválida: ");
            }

            user.Deactivate();

            userDB.Save();

            Console.WriteLine("Usuário desativado com sucesso");
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
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
            Console.Clear();
            Console.WriteLine("Relatório de clientes ativos");
            foreach (var client in clientDB.Clients.Where(c => c.IsActive))
            {
                Console.WriteLine(client);
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void PrintInactiveClients()
        {
            Console.Clear();
            Console.WriteLine("Relatório de clientes inativos");
            foreach (var client in clientDB.Clients.Where(c => !c.IsActive))
            {
                Console.WriteLine(client);
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void PrintActiveUsers()
        {
            Console.Clear();
            Console.WriteLine("Relatório de funcionários ativos");
            foreach (var user in userDB.Users.Where(u => u.IsActive))
            {
                Console.WriteLine(user);
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void PrintFailures()
        {
            Console.Clear();
            Console.WriteLine("Relatório de transações com falha");
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
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
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
