using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemC
{
    public class StaticHashFile
    {
        private Dictionary<int, Block> _storage = new Dictionary<int, Block>();

        private int _primaryBlocksCount;
        private int _blockingFactor;
        private int _nextOverflowAddress;

        public int ReadCount { get; private set; }
        public int WriteCount { get; private set; }

        public StaticHashFile(int primaryBlocksCount, int blockingFactor)
        {
            _primaryBlocksCount = primaryBlocksCount;
            _blockingFactor = blockingFactor;
            _nextOverflowAddress = primaryBlocksCount; 

            for (int i = 0; i < primaryBlocksCount; i++)
            {
                WriteBlock(new Block(i, blockingFactor));
            }
        }

        private Block ReadBlock(int address)
        {
            ReadCount++;
            return _storage.ContainsKey(address) ? _storage[address] : null;
        }

        private void WriteBlock(Block block)
        {
            WriteCount++;
            _storage[block.Address] = block;
        }

        private int GetHash(string key)
        {
            int hash = 0;
            foreach (char c in key)
            {
                hash = (hash * 31 + c) % _primaryBlocksCount;
            }
            return Math.Abs(hash);
        }

        public bool Insert(Record record)
        {
            if (Search(record.Name) != null) return false; 

            int address = GetHash(record.Name);
            Block currentBlock = ReadBlock(address);

            while (true)
            {
                if (!currentBlock.IsFull)
                {
                    currentBlock.Records.Add(record);
                    WriteBlock(currentBlock);
                    return true;
                }

                if (currentBlock.NextBlockAddress == -1)
                {
                    Block newOverflowBlock = new Block(_nextOverflowAddress++, _blockingFactor);
                    currentBlock.NextBlockAddress = newOverflowBlock.Address;
                    WriteBlock(currentBlock); 

                    newOverflowBlock.Records.Add(record);
                    WriteBlock(newOverflowBlock); 
                    return true;
                }
                else
                {
                    currentBlock = ReadBlock(currentBlock.NextBlockAddress);
                }
            }
        }

        public Record Search(string key)
        {
            int address = GetHash(key);
            Block currentBlock = ReadBlock(address);

            while (currentBlock != null)
            {
                var record = currentBlock.Records.FirstOrDefault(r => r.Name == key);
                if (record != null) return record;

                if (currentBlock.NextBlockAddress == -1) break;
                currentBlock = ReadBlock(currentBlock.NextBlockAddress);
            }
            return null;
        }

        public bool Delete(string key)
        {
            int address = GetHash(key);
            Block currentBlock = ReadBlock(address);

            while (currentBlock != null)
            {
                var record = currentBlock.Records.FirstOrDefault(r => r.Name == key);
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

        public List<Record> GetAllRecords()
        {
            List<Record> allRecords = new List<Record>();
            for (int i = 0; i < _primaryBlocksCount; i++)
            {
                Block currentBlock = ReadBlock(i);
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
