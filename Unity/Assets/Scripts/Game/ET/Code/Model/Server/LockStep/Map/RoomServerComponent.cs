using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Room))]
    public class RoomServerComponent: Entity, IAwake<Match2Map_GetRoom>
    {
    }
}