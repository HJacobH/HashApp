using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemC
{
    public class HashRecord<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public HashRecord(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
