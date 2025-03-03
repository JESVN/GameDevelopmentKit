﻿using System;
using Cysharp.Threading.Tasks;

namespace ET.Server
{
	[ActorMessageLocationHandler(SceneType.Map)]
	public class C2M_TestRobotCaseHandler : ActorMessageLocationHandler<Unit, C2M_TestRobotCase, M2C_TestRobotCase>
	{
		protected override async UniTask Run(Unit unit, C2M_TestRobotCase request, M2C_TestRobotCase response)
		{
			response.N = request.N;
			await UniTask.CompletedTask;
		}
	}
}