using System;
using System.Text;

namespace TitanCalc.Modules
{
    public enum WordSize
    {
        Byte = 8,
        Word = 16,
        DWord = 32,
        QWord = 64
    }

    /// <summary>
    /// Base-N / Programmer calculator module for concurrent HEX, DEC, OCT, BIN display
    /// and bitwise logic operations across 8, 16, 32, and 64-bit word sizes.
    /// </summary>
    public class ProgrammerEngine
    {
        private ulong _currentValue;
        private WordSize _wordSize;

        public WordSize WordSize
        {
            get { return _wordSize; }
            set
            {
                _wordSize = value;
                _currentValue &= GetMask();
            }
        }

        public ulong Value
        {
            get { return _currentValue & GetMask(); }
            set { _currentValue = value & GetMask(); }
        }

        public ProgrammerEngine()
        {
            _wordSize = WordSize.QWord;
            _currentValue = 0;
        }

        public ulong GetMask()
        {
            switch (_wordSize)
            {
                case WordSize.Byte: return 0xFFUL;
                case WordSize.Word: return 0xFFFFUL;
                case WordSize.DWord: return 0xFFFFFFFFUL;
                case WordSize.QWord:
                default: return 0xFFFFFFFFFFFFFFFFUL;
            }
        }

        public long ToSigned()
        {
            ulong val = Value;
            switch (_wordSize)
            {
                case WordSize.Byte: return (sbyte)(val & 0xFF);
                case WordSize.Word: return (short)(val & 0xFFFF);
                case WordSize.DWord: return (int)(val & 0xFFFFFFFF);
                case WordSize.QWord:
                default: return (long)val;
            }
        }

        public string ToHex()
        {
            int digits = (int)_wordSize / 4;
            return Value.ToString("X" + digits);
        }

        public string ToDec(bool signed = true)
        {
            if (signed)
                return ToSigned().ToString();
            return Value.ToString();
        }

        public string ToOct()
        {
            return Convert.ToString((long)Value, 8);
        }

        public string ToBin(bool groupNibbles = true)
        {
            int bits = (int)_wordSize;
            StringBuilder sb = new StringBuilder(bits + bits / 4);
            ulong val = Value;

            for (int i = bits - 1; i >= 0; i--)
            {
                ulong bit = (val >> i) & 1UL;
                sb.Append(bit == 1 ? '1' : '0');
                if (groupNibbles && i > 0 && i % 4 == 0)
                {
                    sb.Append(' ');
                }
            }

            return sb.ToString();
        }

        public void SetFromHex(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            Value = ulong.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }

        public void SetFromDec(string dec)
        {
            long s;
            if (long.TryParse(dec, out s))
            {
                Value = (ulong)s;
            }
            else
            {
                Value = ulong.Parse(dec);
            }
        }

        public void SetFromBin(string bin)
        {
            bin = bin.Replace(" ", "").Trim();
            ulong v = 0;
            for (int i = 0; i < bin.Length; i++)
            {
                v <<= 1;
                if (bin[i] == '1') v |= 1;
            }
            Value = v;
        }

        public void SetFromOct(string oct)
        {
            Value = (ulong)Convert.ToInt64(oct.Trim(), 8);
        }

        // Bitwise operations
        public ulong And(ulong operand) { Value &= operand; return Value; }
        public ulong Or(ulong operand) { Value |= operand; return Value; }
        public ulong Xor(ulong operand) { Value ^= operand; return Value; }
        public ulong Not() { Value = ~Value & GetMask(); return Value; }
        public ulong Nand(ulong operand) { Value = ~(Value & operand) & GetMask(); return Value; }
        public ulong Nor(ulong operand) { Value = ~(Value | operand) & GetMask(); return Value; }
        public ulong ShiftLeft(int count) { Value = (Value << count) & GetMask(); return Value; }
        public ulong ShiftRight(int count) { Value = (Value >> count); return Value; }

        public ulong RotateLeft(int count)
        {
            int bits = (int)_wordSize;
            count %= bits;
            ulong v = Value;
            Value = ((v << count) | (v >> (bits - count))) & GetMask();
            return Value;
        }

        public ulong RotateRight(int count)
        {
            int bits = (int)_wordSize;
            count %= bits;
            ulong v = Value;
            Value = ((v >> count) | (v << (bits - count))) & GetMask();
            return Value;
        }
    }
}
