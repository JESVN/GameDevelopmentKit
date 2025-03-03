using Cysharp.Threading.Tasks;

namespace ET.Client
{

    public static partial class LSSceneChangeHelper
    {
        // 场景切换协程
        public static async UniTask SceneChangeTo(Scene clientScene, string sceneName, long sceneInstanceId)
        {
            clientScene.RemoveComponent<Room>();

            Room room = clientScene.AddComponentWithId<Room>(sceneInstanceId);
            room.Name = sceneName;

            // 等待表现层订阅的事件完成
            await EventSystem.Instance.PublishAsync(clientScene, new EventType.LSSceneChangeStart() {Room = room});

            clientScene.GetComponent<SessionComponent>().Session.Send(new C2Room_ChangeSceneFinish());
            
            // 等待Room2C_EnterMap消息
            WaitType.Wait_Room2C_Start waitRoom2CStart = await clientScene.GetComponent<ObjectWait>().Wait<WaitType.Wait_Room2C_Start>();

            room.LSWorld = new LSWorld(SceneType.LockStepClient);
            room.Init(waitRoom2CStart.Message.UnitInfo, waitRoom2CStart.Message.StartTime);
            
            room.AddComponent<LSClientUpdater>();

            // 这个事件中可以订阅取消loading
            EventSystem.Instance.Publish(clientScene, new EventType.LSSceneInitFinish());
        }
        
        // 场景切换协程
        public static async UniTask SceneChangeToReplay(Scene clientScene, Replay replay)
        {
            clientScene.RemoveComponent<Room>();

            Room room = clientScene.AddComponent<Room>();
            room.Name = "Map1";
            room.IsReplay = true;
            room.Replay = replay;
            room.LSWorld = new LSWorld(SceneType.LockStepClient);
            room.Init(replay.UnitInfos, TimeHelper.ServerFrameTime());
            
            // 等待表现层订阅的事件完成
            await EventSystem.Instance.PublishAsync(clientScene, new EventType.LSSceneChangeStart() {Room = room});
            

            room.AddComponent<LSReplayUpdater>();
            // 这个事件中可以订阅取消loading
            EventSystem.Instance.Publish(clientScene, new EventType.LSSceneInitFinish());
        }
        
        // 场景切换协程
        public static async UniTask SceneChangeToReconnect(Scene clientScene, G2C_Reconnect message)
        {
            clientScene.RemoveComponent<Room>();

            Room room = clientScene.AddComponent<Room>();
            room.Name = "Map1";
            
            room.LSWorld = new LSWorld(SceneType.LockStepClient);
            room.Init(message.UnitInfos, message.StartTime, message.Frame);
            
            // 等待表现层订阅的事件完成
            await EventSystem.Instance.PublishAsync(clientScene, new EventType.LSSceneChangeStart() {Room = room});


            room.AddComponent<LSClientUpdater>();
            // 这个事件中可以订阅取消loading
            EventSystem.Instance.Publish(clientScene, new EventType.LSSceneInitFinish());
        }
    }
}