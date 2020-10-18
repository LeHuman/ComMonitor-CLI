using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComMonitor
{
    public enum DataType
    {
        A,
        H,
        D,
        B,
        Ascii,
        Hex,
        Decimal,
        Dec,
        Binary,
        Bin,
    }

    public static class SerialType
    {

        private static DataType defaultType = DataType.A;

        public static void setDefault(DataType type)
        {
            defaultType = type;
        }
        public static DataType getDefault()
        {
            return defaultType;
        }

        public static String getDefaultType(byte[] data)
        {
            switch (defaultType)
            {
                case DataType.A:
                case DataType.Ascii:
                    return getAscii(data);
                case DataType.H:
                case DataType.Hex:
                    return getHex(data);
                case DataType.D:
                case DataType.Dec:
                case DataType.Decimal:
                    return getDecimal(data);
                case DataType.B:
                case DataType.Bin:
                case DataType.Binary:
                    return getBinary(data);
                default:
                    return "";
            }
        }

        private static char[] lookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static string ToHex(this byte[] data, char separator)
        {
            int i, p = 0, l = data.Length;
            char[] c = new char[l * 2 + l];
            byte d;
            i = -1;
            --l;
            --p;
            while (i < l)
            {
                d = data[++i];
                c[++p] = lookup[d >> 4];
                c[++p] = lookup[d & 0xF];
                c[++p] = separator;
            }
            return new string(c, 0, c.Length);
        }

        public static String getAscii(byte[] data)
        {
            return Encoding.ASCII.GetString(data);
        }

        public static String getHex(byte[] data)
        {
            return ToHex(data, ' ');
        }

        public static String getDecimal(byte[] data)
        {
            StringBuilder fnl = new StringBuilder();

            foreach (byte i in data)
            {
                fnl.Append(i.ToString().PadLeft(3, ' ')).Append(' ');
            }

            return fnl.ToString();
        }

        public static String getBinary(byte[] data)
        {
            StringBuilder fnl = new StringBuilder();

            foreach (byte i in data)
            {
                fnl.Append(Convert.ToString(i, 2).PadLeft(8, '0')).Append(" ");
            }

            return fnl.ToString();
        }
    }
}
