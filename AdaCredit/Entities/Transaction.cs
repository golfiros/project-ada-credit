using System;

using CsvHelper.Configuration;

namespace AdaCredit.Entities
{
    enum TransactionResult
    {
        INVALID_SOURCE,
        INVALID_TARGET,
        INVALID_TYPE,
        INSUFFICIENT_BALANCE
    }

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

        public decimal Tariff(DateOnly date)
        {
            if (date < new DateOnly(2022, 12, 01))
            {
                return 0;
            }
            return Type switch
            {
                TransactionType.TED => 5m,
                TransactionType.DOC => 1m + Math.Min(5m, Amount * 0.01m),
                TransactionType.TEF => 0m,
                _ => throw new ArgumentOutOfRangeException(nameof(Type), $"Unexpected type value: {Type}")
            };
        }

        public override string ToString()
        {
            return
                $"b. {sBank:000} ag. {sBranch:0000} c.c. {sAccount:00000-0} -> " +
                $"b. {tBank:000} ag. {tBranch:0000} c.c. {tAccount:00000-0} : " +
                $"{Type} - R${Amount:F2}";
        }
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
