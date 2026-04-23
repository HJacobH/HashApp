using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemC
{
    public class StaticHashFile<TKey, TValue>
    {
        private Dictionary<int, Block<TKey, TValue>> _storage = new Dictionary<int, Block<TKey, TValue>>();

        private readonly int _primaryBlocksCount;
        private readonly int _blockingFactor;
        private int _nextOverflowAddress;

        private readonly Func<TKey, int, int> _hashFunction;

        public int ReadCount { get; private set; }
        public int WriteCount { get; private set; }

        public StaticHashFile(int primaryBlocksCount, int blockingFactor, Func<TKey, int, int> hashFunction)
        {
            _primaryBlocksCount = primaryBlocksCount;
            _blockingFactor = blockingFactor;
            _nextOverflowAddress = primaryBlocksCount;
            _hashFunction = hashFunction ?? throw new ArgumentNullException(nameof(hashFunction));

            for (int i = 0; i < primaryBlocksCount; i++)
            {
                WriteBlock(new Block<TKey, TValue>(i, blockingFactor));
            }
        }

        private Block<TKey, TValue> ReadBlock(int address)
        {
            ReadCount++;
            return _storage.ContainsKey(address) ? _storage[address] : null;
        }

        private void WriteBlock(Block<TKey, TValue> block)
        {
            WriteCount++;
            _storage[block.Address] = block;
        }

        private int GetHash(TKey key)
        {
            return _hashFunction(key, _primaryBlocksCount);
        }

        public bool Insert(TKey key, TValue value)
        {
            if (Search(key) != null) return false;

            int address = GetHash(key);
            Block<TKey, TValue> currentBlock = ReadBlock(address);

            while (true)
            {
                if (!currentBlock.IsFull)
                {
                    currentBlock.Records.Add(new HashRecord<TKey, TValue>(key, value));
                    WriteBlock(currentBlock);
                    return true;
                }

                if (currentBlock.NextBlockAddress == -1)
                {
                    Block<TKey, TValue> newOverflowBlock = new Block<TKey, TValue>(_nextOverflowAddress++, _blockingFactor);
                    currentBlock.NextBlockAddress = newOverflowBlock.Address;
                    WriteBlock(currentBlock);

                    newOverflowBlock.Records.Add(new HashRecord<TKey, TValue>(key, value));
                    WriteBlock(newOverflowBlock);
                    return true;
                }
                else
                {
                    currentBlock = ReadBlock(currentBlock.NextBlockAddress);
                }
            }
        }

        public HashRecord<TKey, TValue> Search(TKey key)
        {
            int address = GetHash(key);
            Block<TKey, TValue> currentBlock = ReadBlock(address);

            while (currentBlock != null)
            {
                var record = currentBlock.Records.FirstOrDefault(r => EqualityComparer<TKey>.Default.Equals(r.Key, key));
                if (record != null) return record;

                if (currentBlock.NextBlockAddress == -1) break;
                currentBlock = ReadBlock(currentBlock.NextBlockAddress);
            }
            return null;
        }

        public bool Delete(TKey key)
        {
            int address = GetHash(key);
            Block<TKey, TValue> currentBlock = ReadBlock(address);

            while (currentBlock != null)
            {
                var record = currentBlock.Records.FirstOrDefault(r => EqualityComparer<TKey>.Default.Equals(r.Key, key));
                if (record != null)
                {
                    currentBlock.Records.Remove(record);
                    WriteBlock(currentBlock);
                    return true;
                }

                if (currentBlock.NextBlockAddress == -1) break;
                currentBlock = ReadBlock(currentBlock.NextBlockAddress);
            }
            return false;
        }

        public List<HashRecord<TKey, TValue>> GetAllRecords()
        {
            List<HashRecord<TKey, TValue>> allRecords = new List<HashRecord<TKey, TValue>>();
            for (int i = 0; i < _primaryBlocksCount; i++)
            {
                Block<TKey, TValue> currentBlock = ReadBlock(i);
                while (currentBlock != null)
                {
                    allRecords.AddRange(currentBlock.Records);
                    if (currentBlock.NextBlockAddress == -1) break;
                    currentBlock = ReadBlock(currentBlock.NextBlockAddress);
                }
            }
            return allRecords;
        }

        public void ResetIOStats()
        {
            ReadCount = 0;
            WriteCount = 0;
        }
    }
}
