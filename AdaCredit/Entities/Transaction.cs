using CsvHelper.Configuration;

namespace AdaCredit.Entities
{
    enum TransactionType
    {
        TED,
        DOC,
        TEF
    }

    internal class Transaction
    {
        public uint sBank { get; init; }
        public uint sBranch { get; init; }
        public uint sAccount { get; init; }

        public uint tBank { get; init; }
        public uint tBranch { get; init; }
        public uint tAccount { get; init; }

        public TransactionType Type { get; init; }

        public decimal Amount { get; init; }
    }

    internal sealed class TransactionMap : ClassMap<Transaction>
    {
        public TransactionMap()
        {
            Map(m => m.sBank).Index(0);
            Map(m => m.sBranch).Index(1);
            Map(m => m.sAccount).Index(2);
            Map(m => m.tBank).Index(3);
            Map(m => m.tBranch).Index(4);
            Map(m => m.tAccount).Index(5);
            Map(m => m.Type).Index(6);
            Map(m => m.Amount).Index(7);
        }
    }
}
