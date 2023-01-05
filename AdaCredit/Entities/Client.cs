using System;

using CsvHelper.Configuration;

namespace AdaCredit.Entities
{
    using Extra;
    internal class Client
    {
        public uint Branch { get; init; }
        public uint Account { get; init; }

        public decimal Balance { get => _balance; init => _balance = value; }
        public bool IsActive { get => _isActive; init => _isActive = value; }

        private decimal _balance;
        private bool _isActive;

        public string? Name { get; set; }
        public CPF Cpf { get; init; }

        public bool ModifyBalance(decimal delta)
        {
            if (_balance + delta < 0 || !_isActive) { return false; }
            _balance += delta;
            return true;
        }

        public void Deactivate() => _isActive = false;

        public override string ToString()
        {
            return
                $"ag. {Branch:0000} c.c. {Account:00000-0} : " +
                (_isActive ? $"R${_balance:F2}" : "(inativa)") +
                $"{Environment.NewLine}{Name}{Environment.NewLine}" +
                $"CPF: {Cpf}{Environment.NewLine}";
        }
    }

    internal sealed class ClientMap : ClassMap<Client>
    {
        public ClientMap()
        {
            Map(m => m.Branch).Index(0);
            Map(m => m.Account).Index(1);
            Map(m => m.Balance).Index(2);
            Map(m => m.IsActive).Index(3);
            Map(m => m.Name).Index(4);
            Map(m => m.Cpf).Index(5).TypeConverter<CPFConverter>();
        }
    }
}
