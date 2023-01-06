using System;

using CsvHelper.Configuration;

namespace AdaCredit.Entities
{
    internal class User
    {
        private static Random rnd = new Random();

        public string Username { get; init; } = "";
        public string? Salt { get => _salt; init => _salt = value; }
        public string? Hash { get => _hash; init => _hash = value; }
        public bool IsActive { get => _isActive; init => _isActive = value; }

        public DateTime LastLogin { get; set; }

        private string? _salt;
        private string? _hash;
        private bool _isActive;

        public User() { }

        public User(string pass)
        {
            // simple 11 char salt
            _salt = Convert.ToBase64String(BitConverter.GetBytes(rnd.NextInt64()));
            _hash = BCrypt.Net.BCrypt.HashPassword(pass + _salt);
        }

        public void ChangePass(string pass)
        {
            _salt = Convert.ToBase64String(BitConverter.GetBytes(rnd.NextInt64()));
            _hash = BCrypt.Net.BCrypt.HashPassword(pass + _salt);
        }

        public bool CheckPass(string pass)
        {
            return BCrypt.Net.BCrypt.Verify(pass + _salt, _hash);
        }

        public void Deactivate()
        {
            _isActive = false;
        }

        public override string ToString()
        {
            return $"Usuário {Username} último visto em {LastLogin}";
        }
    }

    internal sealed class UserMap : ClassMap<User>
    {
        public UserMap()
        {
            Map(m => m.Username).Index(0);
            Map(m => m.Salt).Index(1);
            Map(m => m.Hash).Index(2);
            Map(m => m.IsActive).Index(3);
            Map(m => m.LastLogin).Index(4);
        }
    }
}
