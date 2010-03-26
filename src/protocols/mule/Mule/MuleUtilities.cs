using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;
using System.Diagnostics;

namespace Mule
{
    public class MuleUtilities
    {
        public static bool IsLowID(uint id)
        {
            return (id < 16777216);
        }

        public static uint SwapAlways(uint val)
        {
            uint v1 = val & 0xFFFF;
            uint v2 = (val >> 16) & 0xFFFF;

            return v1 << 16 | v2;
        }

        public static RC4Key RC4CreateKey(byte[] pachKeyData, uint nLen)
        {
            RC4Key key = null;

            return RC4CreateKey(pachKeyData, nLen, ref key, false);
        }

        public static RC4Key RC4CreateKey(byte[] pachKeyData, uint nLen, ref RC4Key key)
        {
            return RC4CreateKey(pachKeyData, nLen, ref key, false);
        }

        public static RC4Key RC4CreateKey(byte[] pachKeyData, uint nLen, ref RC4Key key, bool bSkipDiscard)
        {
            byte index1;
            byte index2;
            byte[] pabyState;

            if (key == null)
                key = new RC4Key();

            pabyState = key.abyState;
            for (int i = 0; i < 256; i++)
                pabyState[i] = (byte)i;

            key.byX = 0;
            key.byY = 0;
            index1 = 0;
            index2 = 0;
            for (int i = 0; i < 256; i++)
            {
                index2 = Convert.ToByte(pachKeyData[index1] + pabyState[i] + index2);
                MpdUtilities.SwapByte(ref pabyState[i], ref pabyState[index2]);
                index1 = (byte)((index1 + 1) % nLen);
            }

            if (!bSkipDiscard)
                RC4Crypt(null, null, 1024, key);

            return key;
        }

        public static void RC4Crypt(byte[] pachIn, byte[] pachOut, uint nLen, RC4Key key)
        {
            RC4Crypt(pachIn, 0, pachOut, 0, nLen, key);
        }

        public static void RC4Crypt(byte[] pachIn, int in_offset, byte[] pachOut, int out_offset, uint nLen, RC4Key key)
        {
            Debug.Assert(key != null && nLen > 0);
            if (key == null)
                return;

            byte byX = key.byX; ;
            byte byY = key.byY;
            byte[] pabyState = key.abyState;
            byte byXorIndex;

            for (uint i = 0; i < nLen; i++)
            {
                byX = Convert.ToByte(byX + 1);
                byY = Convert.ToByte(pabyState[byX] + byY);
                MpdUtilities.SwapByte(ref pabyState[byX], ref pabyState[byY]);
                byXorIndex = Convert.ToByte(pabyState[byX] + pabyState[byY]);

                if (pachIn != null)
                    pachOut[out_offset + i] = Convert.ToByte(pachIn[in_offset + i] ^ pabyState[byXorIndex]);
            }
            key.byX = byX;
            key.byY = byY;
        }

        public const string COLLECTION_FILEEXTENSION = ".emulecollection";
        public static bool HasCollectionExtention(string sFileName)
        {
            if (sFileName.EndsWith(COLLECTION_FILEEXTENSION))
                return true;
            return false;
        }
    }
}
