using System;
using System.Linq;
using System.Text;

namespace ComMonitor.Serial {

    public enum DataType {
        None,
        Ascii,
        Hex,
        Decimal,
        Binary,
        Mapped,
        A = Ascii,
        H = Hex,
        D = Decimal,
        Dec = Decimal,
        B = Binary,
        Bin = Binary,
        M = Mapped,
        Map = Mapped,
    }

    public static class SerialType {
        private static readonly char[] lookup = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'];

        public static string GetHex(byte[] data) {
            return ToHex(data, ' ');
        }

        public static string GetAscii(byte[] data) {
            return Encoding.ASCII.GetString(data);
        }

        public static int GetHexVal(char hex) {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static string GetDecimal(byte[] data) {
            StringBuilder fnl = new();

            foreach (byte i in data) {
                fnl.Append(i.ToString().PadLeft(3, ' ')).Append(' ');
            }

            return fnl.ToString();
        }

        public static string ToHex(this byte[] data, char separator) {
            int i, p = 0, l = data.Length;
            char[] c = new char[l * 2 + l];
            byte d;
            i = -1;
            --l;
            --p;
            while (i < l) {
                d = data[++i];
                c[++p] = lookup[d >> 4];
                c[++p] = lookup[d & 0xF];
                c[++p] = separator;
            }
            return new string(c, 0, c.Length);
        }

        public static string GetTypeData(DataType type, byte[] data) {
            switch (type) {
                case DataType.Ascii:
                    return GetAscii(data);

                case DataType.Hex:
                    return GetHex(data);

                case DataType.Decimal:
                    return GetDecimal(data);

                case DataType.Binary:
                    return GetBinary(data);

                case DataType.None:
                    break;

                case DataType.Mapped:
                    break;

                default:
                    break;
            }
            return "";
        }

        public static byte[] GetByteArray(DataType type, string data) {
            switch (type) {
                case DataType.Ascii:
                    return Encoding.UTF8.GetBytes(data);

                case DataType.Hex:
                    return HexToArray(data);

                case DataType.Decimal:
                    return DecimalToArray(data);

                case DataType.Binary:
                    return BinaryToArray(data);

                case DataType.None:
                    break;

                case DataType.Mapped:
                    break;

                default:
                    break;
            }
            return null;
        }

        public static Func<byte[], string> GetTypeFunction(DataType type) {
            switch (type) {
                case DataType.Ascii:
                    return GetAscii;

                case DataType.Hex:
                    return GetHex;

                case DataType.Decimal:
                    return GetDecimal;

                case DataType.Binary:
                    return GetBinary;

                default:
                    break;
            }
            return null;
        }

        public static string GetBinary(byte[] data) {
            StringBuilder fnl = new();

            foreach (byte i in data) {
                fnl.Append(Convert.ToString(i, 2).PadLeft(8, '0')).Append(' ');
            }

            return fnl.ToString();
        }

        public static byte[] DecimalToArray(string decimalStr) {
            try {
                byte[] fullArr = BitConverter.GetBytes(long.Parse(decimalStr));
                for (int i = fullArr.Length - 1; i >= 0; i--) {
                    if (fullArr[i] != 0)
                        return fullArr.Take(i + 1).ToArray();
                }
                return [0];
            } catch (Exception) {
                Console.WriteLine($"Failed to convert to long byte array: {decimalStr}");
            }
            return null;
        }

        public static byte[] HexToArray(string hex) {
            if (hex.Length % 2 == 1) {
                Console.WriteLine($"The binary key cannot have an odd number of digits: {hex}");
                return null;
            }

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i) {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr.Reverse().ToArray();
        }

        public static byte[] BinaryToArray(string bits) {
            try {
                var numOfBytes = (int)Math.Ceiling(bits.Length / 8m);
                var bytes = new byte[numOfBytes];
                var chunkSize = 8;

                for (int i = 1; i <= numOfBytes; i++) {
                    var startIndex = bits.Length - 8 * i;
                    if (startIndex < 0) {
                        chunkSize = 8 + startIndex;
                        startIndex = 0;
                    }
                    bytes[numOfBytes - i] = Convert.ToByte(bits.Substring(startIndex, chunkSize), 2);
                }
                return bytes.Reverse().ToArray();
            } catch (Exception) {
                Console.WriteLine($"Failed to convert to binary byte array: {bits}");
            }
            return null;
        }
    }
}
