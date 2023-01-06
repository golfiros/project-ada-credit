using System.Collections.Generic;

namespace AdaCredit.Data
{
    using Entities;

    internal class UserDB
    {
        private IRepository<string, User> repo;

        public IEnumerable<User> Users => repo;

        public void Load() => repo.Load();
        public void Save() => repo.Save();

        public UserDB(string filename)
        {
            repo = new CsvRepository<string, User, UserMap>(
                    filename, m => m.Username
                );
        }

        public User? NewUser(string name)
        {
            if (repo.ContainsKey(name)) { return null; }
            var user = new User()
            {
                Username = name,
                IsActive = true
            };
            repo.Add(user);
            return user;
        }

        public User? GetUser(string name) => repo.Get(name);
    }
}
