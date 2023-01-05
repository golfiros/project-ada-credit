using System;
using System.Collections.Generic;

namespace AdaCredit.Data
{
    using Entities;

    internal class ClientDB
    {
        private static Random rnd = new Random();

        private IRepository<(uint, uint), Client> repo;

        public IEnumerable<Client> Clients => repo;
        public IEnumerable<(uint, uint)> Keys => repo.Keys;

        public void Clear() => repo.Clear();
        public void Load() => repo.Load();
        public void Save() => repo.Save();

        public ClientDB(string filename)
        {
            repo = new CsvRepository<(uint, uint), Client, ClientMap>(
                    filename, m => (m.Branch, m.Account)
                );
        }

        public Client NewClient(ClientBase info)
        {
            uint newAccount;
            for (
                newAccount = (uint)rnd.Next(0, 1_000_000);
                repo.ContainsKey((1, newAccount));
                newAccount = (newAccount + 1) % 1_000_000
              ) ;

            Client client = new Client(info)
            {
                Branch = 1,
                Account = newAccount,
                Balance = 0,
                IsActive = true
            };
            repo.Add(client);
            return client;
        }

        public Client? GetClient(uint Branch, uint Account) => repo.Get((Branch, Account));
    }
}
