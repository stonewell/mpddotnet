using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using Mpd.Utilities;
using System.Collections.ObjectModel;
using Mpd.Generic.IO;

namespace Mule.Network.Impl
{
    class PacketImpl : Packet
    {
        #region Inner Classes
        private byte[] UDPHeaderStructToBytes(UDP_Header_Struct head)
        {
            byte[] header = new byte[6];

            header[0] = head.eDonkeyID;
            header[1] = head.command;

            return header;
        }

        private Header_Struct ByteToHeaderStruct(byte[] header)
        {
            Header_Struct head;

            head.eDonkeyID = header[0];
            head.packetlength = BitConverter.ToUInt32(header, 1);
            head.command = header[5];

            return head;
        }

        private byte[] HeaderStructToBytes(Header_Struct head)
        {
            byte[] header = new byte[6];
            header[0] = head.eDonkeyID;

            Array.Copy(BitConverter.GetBytes(head.packetlength),
                0, header, 1, 4);
            header[5] = head.command;

            return header;
        }

        private struct Header_Struct
        {
            public byte eDonkeyID;
            public uint packetlength;
            public byte command;
        };

        private struct UDP_Header_Struct
        {
            public byte eDonkeyID;
            public byte command;
        };
        #endregion

        #region Fields
        protected bool m_bSplitted;
        protected bool m_bLastSplitted;
        protected bool m_bPacked;
        protected byte[] splitted_packet_buffer_;
        #endregion

        #region Constructors
        public PacketImpl()
            : this(MuleConstants.OP_EDONKEYPROT)
        {
        }

        public PacketImpl(byte protocol)
        {
            m_bSplitted = false;
            m_bLastSplitted = false;
            IsFromPartFile = false;
            Size = 0;
            Buffer = null;
            OperationCode = 0x00;
            Protocol = protocol;
            m_bPacked = false;
        }

        public PacketImpl(byte[] header)
        {
            m_bSplitted = false;
            m_bPacked = false;
            m_bLastSplitted = false;
            IsFromPartFile = false;
            Buffer = null;
            Header_Struct head = ByteToHeaderStruct(header);
            Size = head.packetlength - 1;
            OperationCode = (OperationCodeEnum)head.command;
            Protocol = head.eDonkeyID;
        }

        public PacketImpl(byte[] pPacketPart, uint nSize, bool bLast, bool bFromPartFile)
        {// only used for splitted packets!
            IsFromPartFile = bFromPartFile;
            m_bSplitted = true;
            m_bPacked = false;
            m_bLastSplitted = bLast;
            Buffer = null;
            splitted_packet_buffer_ = pPacketPart;
            Size = nSize - 6;
            OperationCode = 0x00;
            Protocol = 0x00;
        }


        public PacketImpl(OperationCodeEnum in_opcode, uint in_size) :
            this(in_opcode, in_size, MuleConstants.OP_EDONKEYPROT, true)
        {
        }

        public PacketImpl(OperationCodeEnum in_opcode, uint in_size, byte protocol) :
            this(in_opcode, in_size, protocol, true)
        {
        }

        public PacketImpl(OperationCodeEnum opCode, uint in_size, byte protocol, bool bFromPartFile)
        {
            IsFromPartFile = bFromPartFile;
            m_bSplitted = false;
            m_bPacked = false;
            m_bLastSplitted = false;
            if (in_size > 0)
            {
                Buffer = new byte[in_size];
                Array.Clear(Buffer, 0, (int)in_size);
            }
            else
            {
                Buffer = null;
            }

            OperationCode = opCode;
            Size = in_size;
            Protocol = protocol;
        }

    	public PacketImpl(SafeMemFile datafile) :
            this(datafile, MuleConstants.OP_EDONKEYPROT, (OperationCodeEnum)0x00)
        {
        }

        public PacketImpl(SafeMemFile datafile, byte protocol) :
            this(datafile, protocol, (OperationCodeEnum)0x00)
        {
        }

        public PacketImpl(SafeMemFile datafile, byte protocol, OperationCodeEnum ucOpcode)
        {
            m_bSplitted = false;
            m_bPacked = false;
            m_bLastSplitted = false;
            IsFromPartFile = false;
            Size = (uint)datafile.Length;
            Buffer = new byte[Size];
            Array.Copy(datafile.Buffer, Buffer, (uint)Size);
            OperationCode = ucOpcode;
            Protocol = protocol;
        }

        public PacketImpl(string str, byte ucProtocol, OperationCodeEnum ucOpcode)
        {
            m_bSplitted = false;
            m_bPacked = false;
            m_bLastSplitted = false;
            IsFromPartFile = false;
            Buffer = Encoding.ASCII.GetBytes(str);
            Size = (uint)Buffer.Length;
            OperationCode = ucOpcode;
            Protocol = ucProtocol;
        }
        #endregion

        #region Packet Members

        public virtual byte[] Header
        {
            get
            {
                Debug.Assert(!m_bSplitted);

                Header_Struct header;
                header.command = (byte)OperationCode;
                header.eDonkeyID = Protocol;
                header.packetlength = Size + 1;
                return HeaderStructToBytes(header);
            }
        }

        public virtual byte[] UDPHeader
        {
            get 
            {
                Debug.Assert(!m_bSplitted);
                UDP_Header_Struct header;
                header.command = (byte)OperationCode;
                header.eDonkeyID = Protocol;
                return UDPHeaderStructToBytes(header);
            }
        }

        public virtual byte[] Packet
        {
            get 
            {
                if (m_bSplitted && splitted_packet_buffer_ != null)
                {
                    return splitted_packet_buffer_;
                }
                else
                {
                    byte[] tempbuffer = new byte[Size + 10];
                    Array.Copy(Header, tempbuffer, 6);
                    Array.Copy(Buffer, 0, tempbuffer, 6, Size);
                    return tempbuffer;
                }
            }
        }

        public virtual byte[] DetachPacket()
        {
            if (m_bSplitted && splitted_packet_buffer_ != null)
            {
                byte[] result = splitted_packet_buffer_;
                splitted_packet_buffer_ = null;
                Buffer = null;
                return result;
            }
            else
            {
                byte[] tempbuffer = new byte[Size + 10];
                Array.Copy(Header, tempbuffer, 6);
                Array.Copy(Buffer, 0, tempbuffer, 6, Size);
                return tempbuffer;
            }
        }

        public virtual uint RealPacketSize
        {
            get { return Size + 6; }
        }

        public bool IsFromPartFile
        {
            get;
            set;
        }

        public void PackPacket()
        {
            Debug.Assert(!m_bSplitted);
            byte[] output = null;

            bool result = MpdUtilities.Compress(Buffer, Size, out output);
            if (!result || Size <= output.Length)
            {
                return;
            }

            if (Protocol == MuleConstants.OP_KADEMLIAHEADER)
                Protocol = MuleConstants.OP_KADEMLIAPACKEDPROT;
            else
                Protocol = MuleConstants.OP_PACKEDPROT;
            Array.Copy(output, Buffer, output.Length);
            Size = (uint)output.Length;
            m_bPacked = true;
        }

        public bool UnPackPacket()
        {
            return UnPackPacket(50000);
        }

        public bool UnPackPacket(uint uMaxDecompressedSize)
        {
            Debug.Assert(Protocol == MuleConstants.OP_PACKEDPROT || 
                Protocol == MuleConstants.OP_KADEMLIAPACKEDPROT);

            byte[] unpack = null;

            bool result = MpdUtilities.Decompress(Buffer, Size, out unpack);

            if (result && unpack.Length < uMaxDecompressedSize)
            {
                Debug.Assert(Buffer != null);
                Size = (uint)unpack.Length;
                Buffer = unpack;
                if (Protocol == MuleConstants.OP_KADEMLIAPACKEDPROT)
                    Protocol = MuleConstants.OP_KADEMLIAHEADER;
                else
                    Protocol = MuleConstants.OP_EMULEPROT;
                
                m_bPacked = false;

                return true;
            }
            return false;
        }

        public uint Size
        {
            get;
            set;
        }

        public OperationCodeEnum OperationCode
        {
            get;
            set;
        }

        public byte Protocol
        {
            get;
            set;
        }

        public byte[] Buffer
        {
            get;
            set;
        }

        #endregion
    }
}
