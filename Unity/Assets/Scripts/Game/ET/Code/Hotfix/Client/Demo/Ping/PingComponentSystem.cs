using System;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    public static partial class PingComponentSystem
    {
        [EntitySystem]
        private class PingComponentAwakeSystem : AwakeSystem<PingComponent>
        {
            protected override void Awake(PingComponent self)
            {
                self.PingAsync().Forget();
            }
        }

        [EntitySystem]
        private class PingComponentDestroySystem : DestroySystem<PingComponent>
        {
            protected override void Destroy(PingComponent self)
            {
                self.Ping = default;
            }
        }

        private static async UniTask PingAsync(this PingComponent self)
        {
            Session session = self.GetParent<Session>();
            long instanceId = self.InstanceId;
            
            while (true)
            {
                if (self.InstanceId != instanceId)
                {
                    return;
                }

                long time1 = TimeHelper.ClientNow();
                try
                {
                    C2G_Ping c2GPing = C2G_Ping.Create(true);
                    using G2C_Ping response = await session.Call(c2GPing) as G2C_Ping;

                    if (self.InstanceId != instanceId)
                    {
                        return;
                    }

                    long time2 = TimeHelper.ClientNow();
                    self.Ping = time2 - time1;
                    
                    TimeInfo.Instance.ServerMinusClientTime = response.Time + (time2 - time1) / 2 - time2;
                    
                    await TimerComponent.Instance.WaitAsync(2000);
                }
                catch (RpcException e)
                {
                    // session断开导致ping rpc报错，记录一下即可，不需要打成error
                    Log.Info($"ping error: {self.Id} {e.Error}");
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"ping error: \n{e}");
                }
            }
        }
    }
}