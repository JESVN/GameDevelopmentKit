using TrueSync;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof (LSClientUpdater))]
    public static partial class LSOperaComponentSystem
    {
        [EntitySystem]
        private class LSOperaComponentUpdateSystem : UpdateSystem<LSOperaComponent>
        {
            protected override void Update(LSOperaComponent self)
            {
                TSVector2 v = new();
                if (Input.GetKey(KeyCode.W))
                {
                    v.y += 1;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    v.x -= 1;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    v.y -= 1;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    v.x += 1;
                }

                LSClientUpdater lsClientUpdater = self.GetParent<Room>().GetComponent<LSClientUpdater>();
                lsClientUpdater.Input.V = v.normalized;
            }
        }
    }
}