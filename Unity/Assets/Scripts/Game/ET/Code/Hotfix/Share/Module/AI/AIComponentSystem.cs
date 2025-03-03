using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(AIComponent))]
    [FriendOf(typeof(AIDispatcherComponent))]
    public static partial class AIComponentSystem
    {
        [Invoke(TimerInvokeType.AITimer)]
        public class AITimer: ATimer<AIComponent>
        {
            protected override void Run(AIComponent self)
            {
                try
                {
                    self.Check();
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: {self.Id}\n{e}");
                }
            }
        }
    
        [EntitySystem]
        private class AIComponentAwakeSystem : AwakeSystem<AIComponent, int>
        {
            protected override void Awake(AIComponent self, int aiConfigId)
            {
                self.AIConfigId = aiConfigId;
                self.Timer = TimerComponent.Instance.NewRepeatedTimer(1000, TimerInvokeType.AITimer, self);
            }
        }

        [EntitySystem]
        private class AIComponentDestroySystem : DestroySystem<AIComponent>
        {
            protected override void Destroy(AIComponent self)
            {
                TimerComponent.Instance?.Remove(ref self.Timer);
                self.CancellationToken?.Cancel();
                self.CancellationToken = null;
                self.Current = 0;
            }
        }

        public static void Check(this AIComponent self)
        {
            if (self.Parent == null)
            {
                TimerComponent.Instance.Remove(ref self.Timer);
                return;
            }

            var oneAI = Tables.Instance.DTAIConfig.AIConfigs[self.AIConfigId];

            foreach (DRAIConfig aiConfig in oneAI.Values)
            {

                AIDispatcherComponent.Instance.AIHandlers.TryGetValue(aiConfig.Name, out AAIHandler aaiHandler);

                if (aaiHandler == null)
                {
                    Log.Error($"not found aihandler: {aiConfig.Name}");
                    continue;
                }

                int ret = aaiHandler.Check(self, aiConfig);
                if (ret != 0)
                {
                    continue;
                }

                if (self.Current == aiConfig.Id)
                {
                    break;
                }

                self.Cancel(); // 取消之前的行为
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                self.CancellationToken = cancellationToken;
                self.Current = aiConfig.Id;

                aaiHandler.Execute(self, aiConfig, cancellationToken).Forget();
                return;
            }
            
        }

        private static void Cancel(this AIComponent self)
        {
            self.CancellationToken?.Cancel();
            self.Current = 0;
            self.CancellationToken = null;
        }
    }
} 