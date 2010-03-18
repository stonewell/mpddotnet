using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Mule.Network.Impl
{
    class RawPacketImpl : PacketImpl, RawPacket
    {
        #region Constructors
        public RawPacketImpl(string data)
        {
            AttachPacket(Encoding.ASCII.GetBytes(data));
        }

        public RawPacketImpl(byte[] pcData)
        {
            AttachPacket(pcData, 0, pcData.Length, false);
        }

        public RawPacketImpl(byte[] pcData, bool bFromPartFile)
        {
            AttachPacket(pcData, 0, pcData.Length, bFromPartFile);
        }

        public RawPacketImpl(byte[] pcData, int size)
        {
            AttachPacket(pcData, 0, size, false);
        }

        public RawPacketImpl(byte[] pcData, int size, bool bFromPartFile)
        {
            AttachPacket(pcData, 0, size, bFromPartFile);
        }

        public RawPacketImpl(byte[] pcData, int offset, int size)
        {
            AttachPacket(pcData, offset, size, false);
        }

        public RawPacketImpl(byte[] pcData, int offset, int size, bool bFromPartFile)
        {
            AttachPacket(pcData, offset, size, bFromPartFile);
        }
        #endregion

        #region RawPacket Members
        public void AttachPacket(byte[] pcData)
        {
            AttachPacket(pcData, 0, pcData.Length, false);
        }

        public void AttachPacket(byte[] pcData, bool bFromPartFile)
        {
            AttachPacket(pcData, 0, pcData.Length, bFromPartFile);
        }

        public void AttachPacket(byte[] pcData, int size)
        {
            AttachPacket(pcData, 0, size, false);
        }

        public void AttachPacket(byte[] pcData, int size, bool bFromPartFile)
        {
            AttachPacket(pcData, 0, size, bFromPartFile);
        }

        public void AttachPacket(byte[] pcData, int offset, int size)
        {
            AttachPacket(pcData, offset, size, false);
        }

        public void AttachPacket(byte[] pcData, int offset, int size, bool bFromPartFile)
        {
            Protocol = 0x00;
            Size = (uint)size;
            Buffer = new byte[size];
            Array.Copy(pcData, Buffer, size);
            IsFromPartFile = bFromPartFile;
        }
        #endregion

        #region Overrides
        public override byte[] Header
        {
            get
            {
                Debug.Assert(false);
                return null;
            }
        }

        public override byte[] UDPHeader
        {
            get
            {
                Debug.Assert(false);
                return null;
            }
        }

        public override byte[] Packet
        {
            get
            {
                return Buffer;
            }
        }

        public override byte[] DetachPacket()
        {
            byte[] result = Buffer;
            Buffer = null;
            return result;
        }

        public override uint RealPacketSize
        {
            get
            {
                return Size;
            }
        }
        #endregion
    }
}
