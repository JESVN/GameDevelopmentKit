﻿using System;
using System.Net;
using Cysharp.Threading.Tasks;

namespace ET.Server
{
	[MessageHandler(SceneType.Realm)]
	public class C2R_LoginHandler : MessageHandler<C2R_Login, R2C_Login>
	{
		protected override async UniTask Run(Session session, C2R_Login request, R2C_Login response)
		{
			// 随机分配一个Gate
			DRStartSceneConfig config = RealmGateAddressHelper.GetGate(session.DomainZone(), request.Account);
			Log.Debug($"gate address: {MongoHelper.ToJson(config)}");
			
			// 向gate请求一个key,客户端可以拿着这个key连接gate
			G2R_GetLoginKey g2RGetLoginKey = (G2R_GetLoginKey) await ActorMessageSenderComponent.Instance.Call(
				config.InstanceId, new R2G_GetLoginKey() {Account = request.Account});

			response.Address = config.InnerIPOutPort.ToString();
			response.Key = g2RGetLoginKey.Key;
			response.GateId = g2RGetLoginKey.GateId;
		}
	}
}
