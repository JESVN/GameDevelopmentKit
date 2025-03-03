﻿using System;
using Cysharp.Threading.Tasks;

namespace ET.Server
{
    [FriendOf(typeof(LockInfo))]
    public static partial class LockInfoSystem
    {
        [EntitySystem]
        private class LockInfoAwakeSystem : AwakeSystem<LockInfo, long, CoroutineLock>
        {
            protected override void Awake(LockInfo self, long lockInstanceId, CoroutineLock coroutineLock)
            {
                self.CoroutineLock = coroutineLock;
                self.LockInstanceId = lockInstanceId;
            }
        }

        [EntitySystem]
        private class LockInfoDestroySystem : DestroySystem<LockInfo>
        {
            protected override void Destroy(LockInfo self)
            {
                self.CoroutineLock.Dispose();
                self.LockInstanceId = 0;
            }
        }
    }
    

    
    [FriendOf(typeof(LocationOneType))]
    [FriendOf(typeof(LockInfo))]
    public static partial class LocationOneTypeSystem
    {
        [EntitySystem]
        private class LocationOneTypeAwakeSystem : AwakeSystem<LocationOneType, int>
        {
            protected override void Awake(LocationOneType self, int locationType)
            {
                self.LocationType = locationType;
            }
        }

        public static async UniTask Add(this LocationOneType self, long key, long instanceId)
        {
            int coroutineLockType = (self.LocationType << 16) | CoroutineLockType.Location;
            using (await CoroutineLockComponent.Instance.Wait(coroutineLockType, key))
            {
                self.locations[key] = instanceId;
                Log.Info($"location add key: {key} instanceId: {instanceId}");
            }
        }

        public static async UniTask Remove(this LocationOneType self, long key)
        {
            int coroutineLockType = (self.LocationType << 16) | CoroutineLockType.Location;
            using (await CoroutineLockComponent.Instance.Wait(coroutineLockType, key))
            {
                self.locations.Remove(key);
                Log.Info($"location remove key: {key}");
            }
        }

        public static async UniTask Lock(this LocationOneType self, long key, long instanceId, int time = 0)
        {
            int coroutineLockType = (self.LocationType << 16) | CoroutineLockType.Location;
            CoroutineLock coroutineLock = await CoroutineLockComponent.Instance.Wait(coroutineLockType, key);

            LockInfo lockInfo = self.AddChild<LockInfo, long, CoroutineLock>(instanceId, coroutineLock);
            self.lockInfos.Add(key, lockInfo);

            Log.Info($"location lock key: {key} instanceId: {instanceId}");

            if (time > 0)
            {
                async UniTask TimeWaitAsync()
                {
                    long lockInfoInstanceId = lockInfo.InstanceId;
                    await TimerComponent.Instance.WaitAsync(time);
                    if (lockInfo.InstanceId != lockInfoInstanceId)
                    {
                        return;
                    }
                    Log.Info($"location timeout unlock key: {key} instanceId: {instanceId} newInstanceId: {instanceId}");
                    self.UnLock(key, instanceId, instanceId);
                }
                TimeWaitAsync().Forget();
            }
        }

        public static void UnLock(this LocationOneType self, long key, long oldInstanceId, long newInstanceId)
        {
            if (!self.lockInfos.TryGetValue(key, out LockInfo lockInfo))
            {
                Log.Error($"location unlock not found key: {key} {oldInstanceId}");
                return;
            }

            if (oldInstanceId != lockInfo.LockInstanceId)
            {
                Log.Error($"location unlock oldInstanceId is different: {key} {oldInstanceId}");
                return;
            }

            Log.Info($"location unlock key: {key} instanceId: {oldInstanceId} newInstanceId: {newInstanceId}");

            self.locations[key] = newInstanceId;

            self.lockInfos.Remove(key);

            // 解锁
            lockInfo.Dispose();
        }

        public static async UniTask<long> Get(this LocationOneType self, long key)
        {
            int coroutineLockType = (self.LocationType << 16) | CoroutineLockType.Location;
            using (await CoroutineLockComponent.Instance.Wait(coroutineLockType, key))
            {
                self.locations.TryGetValue(key, out long instanceId);
                Log.Info($"location get key: {key} instanceId: {instanceId}");
                return instanceId;
            }
        }
    }


    [FriendOf(typeof (LocationManagerComponent))]
    public static partial class LocationComponentSystem
    {
        [EntitySystem]
        private class LocationManagerComponentAwakeSystem : AwakeSystem<LocationManagerComponent>
        {
            protected override void Awake(LocationManagerComponent self)
            {
                for (int i = 0; i < self.LocationOneTypes.Length; ++i)
                {
                    self.LocationOneTypes[i] = self.AddChild<LocationOneType, int>(i);
                }
            }
        }

        public static LocationOneType Get(this LocationManagerComponent self, int locationType)
        {
            return self.LocationOneTypes[locationType];
        }
    }
}