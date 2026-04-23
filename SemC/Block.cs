using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemC
{
    public class Block<TKey, TValue>
    {
        public int Address { get; set; }
        public int NextBlockAddress { get; set; } = -1;
        public List<HashRecord<TKey, TValue>> Records { get; set; }
        public int Capacity { get; private set; }

        public Block(int address, int capacity)
        {
            Address = address;
            Capacity = capacity;
            Records = new List<HashRecord<TKey, TValue>>(capacity);
        }

        public bool IsFull => Records.Count >= Capacity;
    }
}
