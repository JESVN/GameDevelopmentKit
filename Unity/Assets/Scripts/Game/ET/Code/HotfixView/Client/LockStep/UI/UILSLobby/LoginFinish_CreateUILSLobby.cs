﻿using Cysharp.Threading.Tasks;

namespace ET.Client
{
	[Event(SceneType.LockStep)]
	public class LoginFinish_CreateUILSLobby: AEvent<Scene, EventType.LoginFinish>
	{
		protected override async UniTask Run(Scene scene, EventType.LoginFinish args)
		{
			await scene.GetComponent<UIComponent>().OpenUIFormAsync(UGFUIFormId.UILSLobby);
		}
	}
}
