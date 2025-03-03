﻿using System;
using Cysharp.Threading.Tasks;

namespace ET.Server
{
	[ActorMessageHandler(SceneType.Gate)]
	public class R2G_GetLoginKeyHandler : ActorMessageHandler<Scene, R2G_GetLoginKey, G2R_GetLoginKey>
	{
		protected override async UniTask Run(Scene scene, R2G_GetLoginKey request, G2R_GetLoginKey response)
		{
			long key = RandomGenerator.RandInt64();
			scene.GetComponent<GateSessionKeyComponent>().Add(key, request.Account);
			response.Key = key;
			response.GateId = scene.Id;
			await UniTask.CompletedTask;
		}
	}
}