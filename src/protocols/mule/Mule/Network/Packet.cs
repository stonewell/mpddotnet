#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;


namespace Mule.Network
{
    public interface Packet
    {
        byte[] Header { get; }
        byte[] UDPHeader { get; }
        byte[] Packet { get; }
        byte[] DetachPacket();
        uint RealPacketSize { get; }
        bool IsFromPartFile { get; }
        void PackPacket();
        bool UnPackPacket(/*UINT uMaxDecompressedSize = 50000*/);
        bool UnPackPacket(uint uMaxDecompressedSize);

        uint Size { get; set;}
        OperationCodeEnum OperationCode { get; set; }
        byte Protocol { get; set; }
        byte[] Buffer { get; set; }
    }

    public interface RawPacket : Packet
    {
        //void AttachPacket(byte[] pcData);
        //void AttachPacket(byte[] pcData, bool bFromPartFile);
        //void AttachPacket(byte[] pcData, int size);
        //void AttachPacket(byte[] pcData, int size, bool bFromPartFile);
        //void AttachPacket(byte[] pcData, int offset, int size);
        //void AttachPacket(byte[] pcData, int offset, int size, bool bFromPartFile);
    };
}
