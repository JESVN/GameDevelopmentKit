using MemoryPack;

namespace ET
{
    [Message(ushort.MaxValue)]
    [MemoryPackable]
    public partial class ActorResponse: ProtoObject, IActorResponse
    {
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        [MemoryPackOrder(3)]
        public string Message { get; set; }
    }
}