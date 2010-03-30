using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Mpd.Xml.Serialization
{
    class XmlConverterEx
    {
        internal static byte[] FromBinHexString(string s)
        {
            char[] chars = s.ToCharArray();
            byte[] bytes = new byte[chars.Length / 2 + chars.Length % 2];
            FromBinHexString(chars, 0, chars.Length, bytes);
            return bytes;
        }

        internal static int FromBinHexString(char[] chars, int offset, int charLength, byte[] buffer)
        {
            int bufIndex = offset;
            for (int i = 0; i < charLength - 1; i += 2)
            {
                buffer[bufIndex] = (chars[i] > '9' ?
                        (byte)(chars[i] - 'A' + 10) :
                        (byte)(chars[i] - '0'));
                buffer[bufIndex] <<= 4;
                buffer[bufIndex] += chars[i + 1] > '9' ?
                        (byte)(chars[i + 1] - 'A' + 10) :
                        (byte)(chars[i + 1] - '0');
                bufIndex++;
            }
            if (charLength % 2 != 0)
                buffer[bufIndex++] = (byte)
                    ((chars[charLength - 1] > '9' ?
                        (byte)(chars[charLength - 1] - 'A' + 10) :
                        (byte)(chars[charLength - 1] - '0'))
                    << 4);

            return bufIndex - offset;
        }

        // LAMESPEC: It has been documented as public, but is marked as internal.
        internal static string ToBinHexString(byte[] buffer)
        {
            StringWriter w = new StringWriter();
            WriteBinHex(buffer, 0, buffer.Length, w);
            return w.ToString();
        }

        internal static void WriteBinHex(byte[] buffer, int index, int count, TextWriter w)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(
#if !NET_2_1
"index", index,
#endif
 "index must be non negative integer.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
#if !NET_2_1
"count", count,
#endif
 "count must be non negative integer.");
            }
            if (buffer.Length < index + count)
                throw new ArgumentOutOfRangeException("index and count must be smaller than the length of the buffer.");

            // Copied from XmlTextWriter.WriteBinHex ()
            int end = index + count;
            for (int i = index; i < end; i++)
            {
                int val = buffer[i];
                int high = val >> 4;
                int low = val & 15;
                if (high > 9)
                    w.Write((char)(high + 55));
                else
                    w.Write((char)(high + 0x30));
                if (low > 9)
                    w.Write((char)(low + 55));
                else
                    w.Write((char)(low + 0x30));
            }
        }
    }
}
