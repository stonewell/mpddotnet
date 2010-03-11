using System;
using System.Collections.Generic;
using System.Text;
using Mule.Core.AICH;
using System.IO;
using Mule.Core.Impl;
using Mule.Core.File;

namespace Mule.Core
{
    class CoreUtilities : MuleBaseObjectImpl
    {
        private readonly byte[] base16Chars =
            Encoding.Default.GetBytes("0123456789ABCDEF");
        private readonly byte[] base32Chars =
            Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567");
        private const int BASE16_LOOKUP_MAX = 23;
        private readonly byte[,] base16Lookup =
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

        public string EncodeBase32(byte[] buffer)
        {
            return EncodeBase32(buffer, 0, buffer.Length);
        }

        public string EncodeBase32(byte[] buffer, int offset, int length)
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

        public string EncodeBase16(byte[] buffer)
        {
            return EncodeBase16(buffer, 0, buffer.Length);
        }

        public string EncodeBase16(byte[] buffer, int offset, int length)
        {
            StringBuilder result = new StringBuilder();

            for (int i = offset; i < length; i++)
            {
                result.Append(base16Chars[buffer[i] >> 4]);
                result.Append(base16Chars[buffer[i] & 0xf]);
            }

            return result.ToString();
        }

        public bool DecodeBase16(char[] base16Buffer, byte[] buffer)
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

        private int DecodeLengthBase16(int base16Length)
        {
            return base16Length / 2;
        }

        public int DecodeBase32(char[] pszInput, byte[] paucOutput)
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

        public int DecodeBase32(char[] pszInput, AICHHash Hash)
        {
            return DecodeBase32(pszInput, Hash.RawHash);
        }

        public UInt16 GetRandomUInt16()
        {
            int rand0 =
                MuleEngine.CoreObjectManager.Random0.Next(CoreConstants.RAND_MAX);
            int rand1 =
                MuleEngine.CoreObjectManager.Random0.Next(CoreConstants.RAND_MAX);

            UInt16 val =
                Convert.ToUInt16(rand0 | (rand1 >= CoreConstants.RAND_MAX / 2 ? 0x8000 : 0x0000));
            return val;
        }

        public bool IsSameDirectory(string dir1, string dir2)
        {
            bool ignore_case =
                Environment.OSVersion.Platform != PlatformID.Unix;

            DirectoryInfo info1 = new DirectoryInfo(dir1);
            DirectoryInfo info2 = new DirectoryInfo(dir2);

            return info1.FullName.Equals(info2.FullName, ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        public bool IsNullMd4(byte[] hash)
        {
            return hash == null || (hash != null &&
                BitConverter.ToUInt32(hash, 0) == 0 &&
                BitConverter.ToUInt32(hash, 4) == 0 &&
                BitConverter.ToUInt32(hash, 8) == 0 &&
                BitConverter.ToUInt32(hash, 12) == 0);
        }

        public int Md4Cmp(byte[] hash1, byte[] hash2)
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

        public void Md4Clr(byte[] hash1)
        {
            Array.Clear(hash1, 0, hash1.Length);
        }

        public void Md4Cpy(byte[] dest, int dstOffset, byte[] src, int srcOffset, int length)
        {
            Array.ConstrainedCopy(src, srcOffset, dest, dstOffset, length);
        }

        public void Md4Cpy(byte[] dest, byte[] src)
        {
            Md4Cpy(dest, 0, src, 0, src.Length);
        }

        public string EncodeHexString(byte[] hash)
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

        public bool DecodeHexString(string strhash, byte[] hash)
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

        public bool IsLowID(uint id)
        {
            return (id < 16777216);
        }

        public bool WriteOptED2KUTF8Tag(Mule.Core.File.FileDataIO file,
            string filename, byte uTagName)
        {
            if (!NeedUTF8String(filename))
                return false;
            Tag tag = MuleEngine.CoreObjectManager.CreateTag(uTagName, filename);
            tag.WriteTagToFile(file, Utf8StrEnum.utf8strOptBOM);
            return true;
        }

        public bool NeedUTF8String(string filename)
        {
            for (int i = 0; i < filename.Length; i++)
            {
                if (filename[i] >= 0x100U)
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly DateTime TIME_FUNC_BEGIN = new DateTime(1970, 1, 1, 0, 0, 0);

        public uint Time()
        {
            return Convert.ToUInt32((DateTime.Now - TIME_FUNC_BEGIN).Seconds);
        }

        public uint DateTime2UInt32Time(DateTime dt)
        {
            return Convert.ToUInt32((dt - TIME_FUNC_BEGIN).Seconds);
        }

        private struct EmuleToED2KMetaTagsMap
        {
            public EmuleToED2KMetaTagsMap(byte id, byte type, string name)
            {
                nID = id;
                nED2KType = type;
                pszED2KName = name;
            }

            public byte nID;
            public byte nED2KType;
            public string pszED2KName;
        };

        public void ConvertED2KTag(ref Tag pTag)
        {
            if (pTag.NameID == 0 && pTag.Name != null)
            {
                EmuleToED2KMetaTagsMap[] _aEmuleToED2KMetaTagsMap = new EmuleToED2KMetaTagsMap[]
		        {
			        // Artist, Album and Title are disabled because they should be already part of the filename
			        // and would therefore be redundant information sent to the servers.. and the servers count the
			        // amount of sent data!
			        new EmuleToED2KMetaTagsMap ( CoreConstants.FT_MEDIA_ARTIST,  CoreConstants.TAGTYPE_STRING, CoreConstants.FT_ED2K_MEDIA_ARTIST ),
			        new EmuleToED2KMetaTagsMap ( CoreConstants.FT_MEDIA_ALBUM,   CoreConstants.TAGTYPE_STRING, CoreConstants.FT_ED2K_MEDIA_ALBUM ),
			        new EmuleToED2KMetaTagsMap ( CoreConstants.FT_MEDIA_TITLE,   CoreConstants.TAGTYPE_STRING, CoreConstants.FT_ED2K_MEDIA_TITLE ),
			        new EmuleToED2KMetaTagsMap ( CoreConstants.FT_MEDIA_LENGTH,  CoreConstants.TAGTYPE_STRING, CoreConstants.FT_ED2K_MEDIA_LENGTH ),
			        new EmuleToED2KMetaTagsMap ( CoreConstants.FT_MEDIA_LENGTH,  CoreConstants.TAGTYPE_UINT32, CoreConstants.FT_ED2K_MEDIA_LENGTH ),
			        new EmuleToED2KMetaTagsMap ( CoreConstants.FT_MEDIA_BITRATE, CoreConstants.TAGTYPE_UINT32, CoreConstants.FT_ED2K_MEDIA_BITRATE ),
			        new EmuleToED2KMetaTagsMap ( CoreConstants.FT_MEDIA_CODEC,   CoreConstants.TAGTYPE_STRING, CoreConstants.FT_ED2K_MEDIA_CODEC )
		        };

                for (int j = 0; j < _aEmuleToED2KMetaTagsMap.Length; j++)
                {
                    if (string.Compare(pTag.Name, _aEmuleToED2KMetaTagsMap[j].pszED2KName) == 0
                        && ((pTag.IsStr && _aEmuleToED2KMetaTagsMap[j].nED2KType == CoreConstants.TAGTYPE_STRING) ||
                        (pTag.IsInt && _aEmuleToED2KMetaTagsMap[j].nED2KType == CoreConstants.TAGTYPE_UINT32)))
                    {
                        if (pTag.IsStr)
                        {
                            if (_aEmuleToED2KMetaTagsMap[j].nID == CoreConstants.FT_MEDIA_LENGTH)
                            {
                                uint nMediaLength = 0;
                                uint hour = 0, min = 0, sec = 0;
                                DateTime dt = DateTime.Now;

                                if (Scan3UInt32(pTag.Str, ref hour, ref min, ref sec) == 3)
                                    nMediaLength = hour * 3600 + min * 60 + sec;
                                else if (Scan2UInt32(pTag.Str, ref min, ref sec) == 2)
                                    nMediaLength = min * 60 + sec;
                                else if (ScanUInt32(pTag.Str, ref sec) == 1)
                                    nMediaLength = sec;

                                if (nMediaLength == 0)
                                    pTag = null;
                                else
                                    pTag =
                                        MuleEngine.CoreObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID,
                                            nMediaLength);
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(pTag.Str))
                                {
                                    pTag =
                                        MuleEngine.CoreObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID, pTag.Str);
                                }
                                else
                                {
                                    pTag = null;
                                }
                            }
                        }
                        else if (pTag.IsInt)
                        {
                            if (pTag.Int != 0)
                            {
                                pTag =
                                    MuleEngine.CoreObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID, pTag.Int);
                            }
                            else
                            {
                                pTag = null;
                            }
                        }
                        break;
                    }
                }
            }
        }

        public int ScanUInt32(string p, ref uint sec)
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

        private int Scan2UInt32(string p, ref uint min, ref uint sec)
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

        private int Scan3UInt32(string p, ref uint hour, ref uint min, ref uint sec)
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

        internal void AdjustNTFSDaylightFileTime(ref uint fdate, string searchpath)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        internal DateTime UInt32ToDateTime(uint seconds)
        {
            return TIME_FUNC_BEGIN + new TimeSpan(0, 0, Convert.ToInt32(seconds));
        }

        internal bool GotoString(Stream file, byte[] find, long plen)
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
    }
}
