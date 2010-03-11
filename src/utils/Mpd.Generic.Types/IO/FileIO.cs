using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpd.Generic.Types.IO
{
    public interface FileIO
    {
        int Read(byte[] lpBuf);
        int Read(byte[] lpBuf, int offset, int length);
        void Write(byte[] lpBuf);
        void Write(byte[] lpBuf, int offset, int length);
        void Flush();
        void Close();
        void Abort();
        void SetLength(long length);

        Int64 Seek(Int64 lOff, System.IO.SeekOrigin nFrom);
        Int64 Position { get; }
        Int64 Length { get; }
    }
}
