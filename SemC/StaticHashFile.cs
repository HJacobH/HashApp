using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SemC
{
    public class StaticHashFile<TKey, TValue>
    {
        private readonly FileStream _diskFile;
        private readonly string _filePath = "databaze_obci.bin"; 
        private readonly int _blockSizeInBytes = 4096;

        private readonly int _primaryBlocksCount;
        private readonly int _blockingFactor;
        private int _nextOverflowAddress;

        private readonly Func<TKey, int, int> _hashFunction;

        private readonly Action<BinaryWriter, HashRecord<TKey, TValue>> _serializeRecord;
        private readonly Func<BinaryReader, HashRecord<TKey, TValue>> _deserializeRecord;

        public int ReadCount { get; private set; }
        public int WriteCount { get; private set; }

        public StaticHashFile(
            int primaryBlocksCount,
            int blockingFactor,
            Func<TKey, int, int> hashFunction,
            Action<BinaryWriter, HashRecord<TKey, TValue>> serializeRecord,
            Func<BinaryReader, HashRecord<TKey, TValue>> deserializeRecord)
        {
            _primaryBlocksCount = primaryBlocksCount;
            _blockingFactor = blockingFactor;
            _nextOverflowAddress = primaryBlocksCount;
            _hashFunction = hashFunction;
            _serializeRecord = serializeRecord;
            _deserializeRecord = deserializeRecord;

            _diskFile = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            if (_diskFile.Length == 0)
            {
                for (int i = 0; i < primaryBlocksCount; i++)
                {
                    WriteBlock(new Block<TKey, TValue>(i, blockingFactor));
                }
            }
            else
            {
                _nextOverflowAddress = (int)(_diskFile.Length / _blockSizeInBytes);
            }
        }

        private Block<TKey, TValue> ReadBlock(int address)
        {
            ReadCount++;
            long offset = (long)address * _blockSizeInBytes;

            if (offset >= _diskFile.Length) return null;

            _diskFile.Seek(offset, SeekOrigin.Begin);

            byte[] buffer = new byte[_blockSizeInBytes];
            _diskFile.Read(buffer, 0, _blockSizeInBytes);

            using (MemoryStream ms = new MemoryStream(buffer))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int readAddress = reader.ReadInt32();

                if (readAddress != address) return null;

                Block<TKey, TValue> block = new Block<TKey, TValue>(readAddress, _blockingFactor);
                block.NextBlockAddress = reader.ReadInt32();

                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                {
                    block.Records.Add(_deserializeRecord(reader));
                }

                return block;
            }
        }

        private void WriteBlock(Block<TKey, TValue> block)
        {
            WriteCount++;
            long offset = (long)block.Address * _blockSizeInBytes;

            byte[] buffer = new byte[_blockSizeInBytes];

            using (MemoryStream ms = new MemoryStream(buffer))
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(block.Address);              
                writer.Write(block.NextBlockAddress);     
                writer.Write(block.Records.Count);        

                foreach (var record in block.Records)
                {
                    _serializeRecord(writer, record);     
                }

                if (ms.Position > _blockSizeInBytes)
                {
                    throw new InvalidOperationException("Kritická chyba: Záznamy se nevejdou do velikosti bloku!");
                }
            }

            _diskFile.Seek(offset, SeekOrigin.Begin);
            _diskFile.Write(buffer, 0, _blockSizeInBytes);
            _diskFile.Flush();
        }

        public void Dispose()
        {
            _diskFile?.Dispose();
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
