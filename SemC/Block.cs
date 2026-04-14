using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemC
{
    public class Block
    {
        public int Address { get; set; }
        public int NextBlockAddress { get; set; } = -1;
        public List<Record> Records { get; set; }
        public int Capacity { get; private set; }

        public Block(int address, int capacity)
        {
            Address = address;
            Capacity = capacity;
            Records = new List<Record>(capacity);
        }

        public bool IsFull => Records.Count >= Capacity;
        public bool IsEmpty => Records.Count == 0;
    }
}
