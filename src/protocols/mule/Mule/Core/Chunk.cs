using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core
{
    public class ChunkList : List<Chunk>
    {
    }

    public class Chunk
    {
        private ushort UnionValue { get; set; }
	    public ushort Part { get;set;}
		public ushort Frequency { get { return UnionValue; } set { UnionValue = value; }}
		public ushort Rank { get { return UnionValue; } set { UnionValue = value; }}
    }
}
