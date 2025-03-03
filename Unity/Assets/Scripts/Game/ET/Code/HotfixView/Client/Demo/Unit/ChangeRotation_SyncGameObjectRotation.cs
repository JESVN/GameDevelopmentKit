using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class ChangeRotation_SyncGameObjectRotation: AEvent<Scene, EventType.ChangeRotation>
    {
        protected override async UniTask Run(Scene scene, EventType.ChangeRotation args)
        {
            Unit unit = args.Unit;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                return;
            }
            Transform transform = gameObjectComponent.GameObject.transform;
            transform.rotation = unit.Rotation;
            await UniTask.CompletedTask;
        }
    }
}
