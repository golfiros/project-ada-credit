using System;
using System.Linq;
using System.Collections.Generic;

namespace AdaCredit.Entities
{
    // wrapper struct around a uint to handle parsing
    internal struct CPF : IEquatable<CPF>
    {
        private uint val; // stores the first 9 digits of the tax number

        public CPF(uint cpf)
        {
            if (cpf >= 1_000_000_000)
            {
                val = 0;
                return;
            }

            val = cpf;
        }

        public static bool TryParse(string input, out CPF cpf)
        {
            cpf = new CPF(0);
            List<uint> digits = input.Where(Char.IsDigit).Select(m => uint.Parse(m.ToString())).ToList();

            // take care of obviously invalid entries
            if (digits.Count != 11) { return false; }
            if (digits.Distinct().Count() == 1) { return false; }

            uint s1 = 0, s2 = 0;
            for (uint i = 0; i < 9; i++)
            {
                s1 += (i + 1) * digits[(int)i];
                s2 += (i + 1) * digits[(int)i + 1];
            }
            s1 %= 11; s2 %= 11;
            s1 %= 10; s2 %= 10;

            // check for errors
            if (s1 != digits[9] || s2 != digits[10]) { return false; }

            // just convert
            for (int i = 0; i < 9; i++)
            {
                cpf.val *= 10;
                cpf.val += (uint)digits[i];
            }
            return true;
        }

        public override bool Equals(object? obj) => obj is CPF cpf && this.Equals(cpf);

        public bool Equals(CPF cpf) => val == cpf.val;

        public override int GetHashCode() => val.GetHashCode();

        public static bool operator ==(CPF lhs, CPF rhs) => lhs.Equals(rhs);

        public static bool operator !=(CPF lhs, CPF rhs) => !(lhs == rhs);

        public override string ToString()
        {
            // compute verifier digits
            uint cpf = val;
            uint s1 = 0, s2 = 0;
            for (uint i = 9; i >= 1; i--)
            {
                uint d = cpf % 10;
                s1 += i * d;
                s2 += (i - 1) * d;
                cpf /= 10;
            }
            s1 %= 11; s1 %= 10;
            s2 += 9 * s1;
            s2 %= 11; s2 %= 10;

            return $"{val:000\\.000\\.000}-{s1}{s2}";
        }
    }
}
