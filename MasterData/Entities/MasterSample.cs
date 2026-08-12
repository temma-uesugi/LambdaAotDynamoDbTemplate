using MasterMemory;
using MessagePack;

//Note:ブロックスコープの必要がある
namespace MasterData.Entities
{
    /// <summary>
    /// マスタのサンプル
    /// </summary>
    [MemoryTable("MasterSample"), MessagePackObject(true)]
    public class MasterSample(int id, string name)
    {
        /// <summary>ID</summary>
        [PrimaryKey]
        public int Id { get; } = id;

        /// <summary>名前</summary>
        public string Name { get; } = name;
    }
}

