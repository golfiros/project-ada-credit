using CsvHelper.Configuration;

namespace AdaCredit.Entities
{
    internal class Client
    {
        public string? Name { get; init; }
        public CPF Cpf { get; init; }
        public uint BranchNumber { get; init; }
        public uint AccountNumber { get; init; }
        public decimal Balance { get; init; }
        public bool IsActive { get; init; }
    }

    internal class ClientMap : ClassMap<Client>
    {
        public ClientMap()
        {
            Map(m => m.Name);
            Map(m => m.Cpf).TypeConverter<CPFConverter>();
            Map(m => m.BranchNumber);
            Map(m => m.AccountNumber);
            Map(m => m.Balance);
            Map(m => m.IsActive);
        }
    }
}
