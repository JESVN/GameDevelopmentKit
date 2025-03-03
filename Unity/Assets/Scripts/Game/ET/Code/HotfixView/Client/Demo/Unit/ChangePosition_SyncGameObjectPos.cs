﻿using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class ChangePosition_SyncGameObjectPos: AEvent<Scene, EventType.ChangePosition>
    {
        protected override async UniTask Run(Scene scene, EventType.ChangePosition args)
        {
            Unit unit = args.Unit;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                return;
            }

            Transform transform = gameObjectComponent.Transform;
            transform.position = unit.Position;
            await UniTask.CompletedTask;
        }
    }
}