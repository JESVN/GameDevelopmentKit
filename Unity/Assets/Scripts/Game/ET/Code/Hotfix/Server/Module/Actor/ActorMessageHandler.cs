﻿using System;
using Cysharp.Threading.Tasks;

namespace ET.Server
{
    [EnableClass]
    public abstract class ActorMessageHandler<E, Message>: IMActorHandler where E : Entity where Message : class, IActorMessage
    {
        protected abstract UniTask Run(E entity, Message message);

        public async UniTask Handle(Entity entity, int fromProcess, object actorMessage)
        {
            if (actorMessage is not Message msg)
            {
                Log.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (Message).Name}");
                return;
            }

            if (entity is not E e)
            {
                Log.Error($"Actor类型转换错误: {entity.GetType().FullName} to {typeof (E).Name} --{typeof (Message).FullName}");
                return;
            }

            await this.Run(e, msg);
        }

        public Type GetRequestType()
        {
            if (typeof (IActorLocationMessage).IsAssignableFrom(typeof (Message)))
            {
                Log.Error($"message is IActorLocationMessage but handler is AMActorHandler: {typeof (Message)}");
            }

            return typeof (Message);
        }

        public Type GetResponseType()
        {
            return null;
        }
    }
    
    
    
    [EnableClass]
    public abstract class ActorMessageHandler<E, Request, Response>: IMActorHandler where E : Entity where Request : MessageObject, IActorRequest where Response : MessageObject, IActorResponse
    {
        protected abstract UniTask Run(E unit, Request request, Response response);

        public async UniTask Handle(Entity entity, int fromProcess, object actorMessage)
        {
            try
            {
                if (actorMessage is not Request request)
                {
                    Log.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (Request).Name}");
                    return;
                }

                if (entity is not E ee)
                {
                    Log.Error($"Actor类型转换错误: {entity.GetType().FullName} to {typeof (E).FullName} --{typeof (Request).FullName}");
                    return;
                }

                int rpcId = request.RpcId;
                Response response = NetServices.Instance.FetchMessage<Response>();
                
                try
                {
                    await this.Run(ee, request, response);
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                    response.Error = ErrorCore.ERR_RpcFail;
                    response.Message = exception.ToString();
                }
                
                response.RpcId = rpcId;
                ActorHandleHelper.Reply(fromProcess, response);
            }
            catch (Exception e)
            {
                throw new Exception($"解释消息失败: {actorMessage.GetType().FullName}", e);
            }
        }

        public Type GetRequestType()
        {
            if (typeof (IActorLocationRequest).IsAssignableFrom(typeof (Request)))
            {
                Log.Error($"message is IActorLocationMessage but handler is AMActorRpcHandler: {typeof (Request)}");
            }

            return typeof (Request);
        }

        public Type GetResponseType()
        {
            return typeof (Response);
        }
    }
}