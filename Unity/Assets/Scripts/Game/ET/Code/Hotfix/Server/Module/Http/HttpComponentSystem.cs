using System;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;

namespace ET.Server
{
    [FriendOf(typeof(HttpComponent))]
    public static partial class HttpComponentSystem
    {
        [EntitySystem]
        private class HttpComponentAwakeSystem : AwakeSystem<HttpComponent, string>
        {
            protected override void Awake(HttpComponent self, string address)
            {
                try
                {
                    self.Load();
                
                    self.Listener = new HttpListener();

                    foreach (string s in address.Split(';'))
                    {
                        if (s.Trim() == "")
                        {
                            continue;
                        }
                        self.Listener.Prefixes.Add(s);
                    }

                    self.Listener.Start();

                    self.Accept().Forget();
                }
                catch (HttpListenerException e)
                {
                    throw new Exception($"请先在cmd中运行: netsh http add urlacl url=http://*:你的address中的端口/ user=Everyone, address: {address}", e);
                }
            }
        }

        [EntitySystem]
        private class HttpComponentDestroySystem : DestroySystem<HttpComponent>
        {
            protected override void Destroy(HttpComponent self)
            {
                self.Listener.Stop();
                self.Listener.Close();
            }
        }

        [EntitySystem]
        private class HttpComponentLoadSystem : LoadSystem<HttpComponent>
        {
            protected override void Load(HttpComponent self)
            {
                self.Load();
            }
        }
        private static void Load(this HttpComponent self)
        {
            self.dispatcher = new Dictionary<string, IHttpHandler>();

            HashSet<Type> types = EventSystem.Instance.GetTypes(typeof (HttpHandlerAttribute));

            SceneType sceneType = (self.Parent as IScene).SceneType;

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(HttpHandlerAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                HttpHandlerAttribute httpHandlerAttribute = (HttpHandlerAttribute)attrs[0];

                if (httpHandlerAttribute.SceneType != sceneType)
                {
                    continue;
                }

                object obj = Activator.CreateInstance(type);

                IHttpHandler ihttpHandler = obj as IHttpHandler;
                if (ihttpHandler == null)
                {
                    throw new Exception($"HttpHandler handler not inherit IHttpHandler class: {obj.GetType().FullName}");
                }
                self.dispatcher.Add(httpHandlerAttribute.Path, ihttpHandler);
            }
        }
        
        public static async UniTaskVoid Accept(this HttpComponent self)
        {
            long instanceId = self.InstanceId;
            while (self.InstanceId == instanceId)
            {
                try
                {
                    HttpListenerContext context = await self.Listener.GetContextAsync();
                    self.Handle(context).Forget();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public static async UniTaskVoid Handle(this HttpComponent self, HttpListenerContext context)
        {
            try
            {
                IHttpHandler handler;
                if (self.dispatcher.TryGetValue(context.Request.Url.AbsolutePath, out handler))
                {
                    await handler.Handle(self.DomainScene(), context);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            context.Request.InputStream.Dispose();
            context.Response.OutputStream.Dispose();
        }
    }
}