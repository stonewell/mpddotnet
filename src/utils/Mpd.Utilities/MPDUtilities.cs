using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Mpd.Utilities
{
    public static class MPDUtilities
    {
        private static readonly Random radom0_ = new Random(0);

        private static readonly byte[] base16Chars =
            Encoding.Default.GetBytes("0123456789ABCDEF");
        private static readonly byte[] base32Chars =
            Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567");
        private const int BASE16_LOOKUP_MAX = 23;
        private static readonly byte[,] base16Lookup =
            new byte[BASE16_LOOKUP_MAX, 2]
            {
                { Encoding.Default.GetBytes("0")[0], 0x0 },
                { Encoding.Default.GetBytes("1")[0], 0x1 },
                { Encoding.Default.GetBytes("2")[0], 0x2 },
                { Encoding.Default.GetBytes("3")[0], 0x3 },
                { Encoding.Default.GetBytes("4")[0], 0x4 },
                { Encoding.Default.GetBytes("5")[0], 0x5 },
                { Encoding.Default.GetBytes("6")[0], 0x6 },
                { Encoding.Default.GetBytes("7")[0], 0x7 },
                { Encoding.Default.GetBytes("8")[0], 0x8 },
                { Encoding.Default.GetBytes("9")[0], 0x9 },
	            { Encoding.Default.GetBytes(":")[0], 0x9 },
                { Encoding.Default.GetBytes(";")[0], 0x9 },
                { Encoding.Default.GetBytes("<")[0], 0x9 },
                { Encoding.Default.GetBytes("=")[0], 0x9 },
                { Encoding.Default.GetBytes(">")[0], 0x9 },
                { Encoding.Default.GetBytes("?")[0], 0x9 },
                { Encoding.Default.GetBytes("@")[0], 0x9 },
                { Encoding.Default.GetBytes("A")[0], 0xA },
                { Encoding.Default.GetBytes("B")[0], 0xB },
                { Encoding.Default.GetBytes("C")[0], 0xC },
                { Encoding.Default.GetBytes("D")[0], 0xD },
                { Encoding.Default.GetBytes("E")[0], 0xE },
                { Encoding.Default.GetBytes("F")[0], 0xF }
            };

        public static string EncodeBase32(byte[] buffer)
        {
            return EncodeBase32(buffer, 0, buffer.Length);
        }

        public static string EncodeBase32(byte[] buffer, int offset, int length)
        {
            StringBuilder result = new StringBuilder();

            int i, index;
            byte word;

            for (i = offset, index = 0; i < length; )
            {

                // Is the current word going to span a byte boundary?
                if (index > 3)
                {
                    word = (byte)(buffer[i] & (0xFF >> index));
                    index = (index + 5) % 8;
                    word <<= index;
                    if (i < length - 1)
                        word |= (byte)(buffer[i + 1] >> (8 - index));

                    i++;
                }
                else
                {
                    word = (byte)((buffer[i] >> (8 - (index + 5))) & 0x1F);
                    index = (index + 5) % 8;
                    if (index == 0)
                        i++;
                }

                result.Append(base32Chars[word]);
            }

            return result.ToString();
        }

        public static string EncodeBase16(byte[] buffer)
        {
            return EncodeBase16(buffer, 0, buffer.Length);
        }

        public static string EncodeBase16(byte[] buffer, int offset, int length)
        {
            StringBuilder result = new StringBuilder();

            for (int i = offset; i < length; i++)
            {
                result.Append(base16Chars[buffer[i] >> 4]);
                result.Append(base16Chars[buffer[i] & 0xf]);
            }

            return result.ToString();
        }

        public static bool DecodeBase16(char[] base16Buffer, byte[] buffer)
        {
            int uDecodeLengthBase16 = DecodeLengthBase16(base16Buffer.Length);

            if (uDecodeLengthBase16 > buffer.Length)
                return false;
            Array.Clear(buffer, 0, uDecodeLengthBase16);

            for (int i = 0; i < base16Buffer.Length; i++)
            {
                int lookup = Char.ToUpper(base16Buffer[i]) - '0';

                // Check to make sure that the given word falls inside a valid range
                byte word = 0;

                if (lookup < 0 || lookup >= BASE16_LOOKUP_MAX)
                    word = 0xFF;
                else
                    word = base16Lookup[lookup, 1];

                if (i % 2 == 0)
                {
                    buffer[i / 2] = (byte)(word << 4);
                }
                else
                {
                    buffer[(i - 1) / 2] |= word;
                }
            }
            return true;
        }

        private static int DecodeLengthBase16(int base16Length)
        {
            return base16Length / 2;
        }

        public static int DecodeBase32(char[] pszInput, byte[] paucOutput)
        {
            if (pszInput == null)
                return 0;

            int nDecodeLen = pszInput.Length * 5 / 8;

            if ((pszInput.Length * 5) % 8 > 0)
                nDecodeLen++;

            int nInputLen = pszInput.Length;

            if (paucOutput == null)
                return nDecodeLen;

            if (nDecodeLen > paucOutput.Length)
                return 0;

            uint nBits = 0;
            int nCount = 0;
            int index = 0;
            int o_index = 0;

            for (int nChars = nInputLen; nChars-- > 0; index++)
            {
                if (pszInput[index] >= 'A' && pszInput[index] <= 'Z')
                    nBits |= (uint)(pszInput[index] - 'A');
                else if (pszInput[index] >= 'a' && pszInput[index] <= 'z')
                    nBits |= (uint)(pszInput[index] - 'a');
                else if (pszInput[index] >= '2' && pszInput[index] <= '7')
                    nBits |= (uint)(pszInput[index] - '2' + 26);
                else
                    return 0;

                nCount += 5;

                if (nCount >= 8)
                {
                    paucOutput[o_index++] = (byte)(nBits >> (nCount - 8));
                    nCount -= 8;
                }

                nBits <<= 5;
            }

            return nDecodeLen;
        }

        public static bool DecodeHexString(string strhash, byte[] hash)
        {
            if (strhash.Length != hash.Length * 2)
                return false;

            for (int i = 0; i < hash.Length; i++)
            {
                if (!Uri.IsHexDigit(strhash[2 * i]) ||
                    !Uri.IsHexDigit(strhash[2 * i + 1]))
                    return false;

                hash[i] = Convert.ToByte((Uri.FromHex(strhash[2 * i]) * 16 +
                    Uri.FromHex(strhash[2 * i + 1])) & 0xFF);
            }

            return true;
        }

        public static string EncodeHexString(byte[] hash)
        {
            StringBuilder sb = new StringBuilder();

            if (hash == null)
                return string.Empty;

            foreach (byte b in hash)
            {
                sb.Append(string.Format("{0:X2}", b));
            }

            return sb.ToString();
        }

        public const int RAND_MAX = 0x7fff;

        public static ushort GetRandomUInt16()
        {
            int rand0 =
                radom0_.Next(RAND_MAX);
            int rand1 =
                radom0_.Next(RAND_MAX);

            ushort val =
                Convert.ToUInt16(rand0 | (rand1 >= RAND_MAX / 2 ? 0x8000 : 0x0000));
            return val;
        }

        public static bool IsSameDirectory(string dir1, string dir2)
        {
            bool ignore_case =
                Environment.OSVersion.Platform != PlatformID.Unix;

            DirectoryInfo info1 = new DirectoryInfo(dir1);
            DirectoryInfo info2 = new DirectoryInfo(dir2);

            return info1.FullName.Equals(info2.FullName, ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        public static bool IsNullMd4(byte[] hash)
        {
            return hash == null || (hash != null &&
                BitConverter.ToUInt32(hash, 0) == 0 &&
                BitConverter.ToUInt32(hash, 4) == 0 &&
                BitConverter.ToUInt32(hash, 8) == 0 &&
                BitConverter.ToUInt32(hash, 12) == 0);
        }

        public static int Md4Cmp(byte[] hash1, byte[] hash2)
        {
            if (hash1 == hash2) return 0;
            if (hash1 == null) return 1;
            if (hash2 == null) return 1;

            if (BitConverter.ToUInt32(hash1, 0) == BitConverter.ToUInt32(hash2, 0) &&
                BitConverter.ToUInt32(hash1, 4) == BitConverter.ToUInt32(hash2, 0) &&
                BitConverter.ToUInt32(hash1, 8) == BitConverter.ToUInt32(hash2, 0) &&
                BitConverter.ToUInt32(hash1, 12) == BitConverter.ToUInt32(hash2, 0))
            {
                return 0;
            }

            return 1;
        }

        public static void Md4Clr(byte[] hash1)
        {
            Array.Clear(hash1, 0, hash1.Length);
        }

        public static void Md4Cpy(byte[] dest, int dstOffset, byte[] src, int srcOffset, int length)
        {
            Array.ConstrainedCopy(src, srcOffset, dest, dstOffset, length);
        }

        public static void Md4Cpy(byte[] dest, byte[] src)
        {
            Md4Cpy(dest, 0, src, 0, src.Length);
        }

        private static readonly DateTime TIME_FUNC_BEGIN = new DateTime(1970, 1, 1, 0, 0, 0);

        public static uint Time()
        {
            return Convert.ToUInt32((DateTime.Now - TIME_FUNC_BEGIN).Seconds);
        }

        public static uint DateTime2UInt32Time(DateTime dt)
        {
            return Convert.ToUInt32((dt - TIME_FUNC_BEGIN).Seconds);
        }

        public static void AdjustNTFSDaylightFileTime(ref uint fdate, string searchpath)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public static DateTime UInt32ToDateTime(uint seconds)
        {
            return TIME_FUNC_BEGIN + new TimeSpan(0, 0, Convert.ToInt32(seconds));
        }

        public static int ScanUInt32(string p, ref uint sec)
        {
            try
            {
                Scanner scanner = new Scanner();

                object[] targets = scanner.Scan(p, "{uint}");

                if (targets.Length > 0)
                {
                    sec = Convert.ToUInt32(targets[0]);
                    return 1;
                }
            }
            catch (ScanExeption)
            {
            }

            sec = 0;
            return 0;
        }

        public static int Scan2UInt32(string p, ref uint min, ref uint sec)
        {
            try
            {
                Scanner scanner = new Scanner();

                object[] targets = scanner.Scan(p, "{uint} : {uint}");

                if (targets.Length > 1)
                {
                    min = Convert.ToUInt32(targets[0]);
                    sec = Convert.ToUInt32(targets[1]);
                    return 2;
                }
            }
            catch (ScanExeption)
            {
            }

            min = 0;
            sec = 0;
            return 0;
        }

        public static int Scan3UInt32(string p, ref uint hour, ref uint min, ref uint sec)
        {
            try
            {
                Scanner scanner = new Scanner();

                object[] targets = scanner.Scan(p, "{uint} : {uint} : {uint}");

                if (targets.Length > 2)
                {
                    hour = Convert.ToUInt32(targets[0]);
                    min = Convert.ToUInt32(targets[1]);
                    sec = Convert.ToUInt32(targets[2]);
                    return 3;
                }
            }
            catch (ScanExeption)
            {
            }

            hour = 0;
            min = 0;
            sec = 0;
            return 0;
        }

        public static bool GotoString(Stream file, byte[] find, long plen)
        {
            bool found = false;
            long i = 0;
            long j = 0;
            long len = file.Length - file.Position;
            byte temp;


            while (!found && i < len)
            {
                temp = Convert.ToByte(file.ReadByte());
                if (temp == find[j])
                    j++;
                else if (temp == find[0])
                    j = 1;
                else
                    j = 0;
                if (j == plen)
                    return true;
                i++;
            }
            return false;
        }
        public static void HeapSort(ref List<ushort> count, int first, int last)
        {
            int r;
            for (r = first; (r & int.MinValue) == 0 && (r << 1) < last; )
            {
                int r2 = (r << 1) + 1;
                if (r2 != last)
                    if (count[r2] < count[r2 + 1])
                        r2++;
                if (count[r] < count[r2])
                {
                    ushort t = count[r2];
                    count[r2] = count[r];
                    count[r] = t;
                    r = r2;
                }
                else
                    break;
            }
        }

        public static uint GetTickCount()
        {
            return 0;
        }
    }
}
