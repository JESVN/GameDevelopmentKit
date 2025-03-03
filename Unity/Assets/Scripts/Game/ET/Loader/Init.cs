﻿using System;
using CommandLine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET
{
    [DisallowMultipleComponent]
    public class Init : MonoBehaviour
    {
        public static Init Instance
        {
            get;
            private set;
        }

        private class Runner : MonoBehaviour
        {
            private void Update()
            {
                Game.Update();
            }

            private void LateUpdate()
            {
                Game.LateUpdate();
                Game.FrameFinishUpdate();
            }

            private void OnDestroy()
            {
                EventSystem.Instance.Invoke(new EventType.OnShutdown());
                Game.Close();
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                EventSystem.Instance.Invoke(new EventType.OnApplicationPause(pauseStatus));
            }

            private void OnApplicationFocus(bool hasFocus)
            {
                EventSystem.Instance.Invoke(new EventType.OnApplicationFocus(hasFocus));
            }
        }

        private Runner m_RunnerComponent;

        private void Awake()
        {
            Instance = this;
#if UNITY_ET_VIEW && UNITY_EDITOR
            Entity.SetRootView(this.transform);
#endif
        }

        private void Start()
        {
            StartAsync().Forget();
        }

        private void OnDestroy()
        {
            if (this.m_RunnerComponent != null)
            {
                DestroyImmediate(this.m_RunnerComponent);
            }
        }

        private async UniTaskVoid StartAsync()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => { Log.Error(e.ExceptionObject.ToString()); };

            Game.AddSingleton<MainThreadSynchronizationContext>();

            Game.AddSingleton<GlobalComponent>();

            // 命令行参数
            string[] args = "".Split(" ");
            Parser.Default.ParseArguments<Options>(args)
                    .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                    .WithParsed(Game.AddSingleton);

            Options.Instance.StartConfig = "Localhost";

            Game.AddSingleton<TimeInfo>().ITimeNow = new UnityTimeNow();
            Game.AddSingleton<Logger>().ILog = new UnityLogger();
            Game.AddSingleton<ObjectPool>();
            Game.AddSingleton<IdGenerater>();
            Game.AddSingleton<EventSystem>();
            Game.AddSingleton<TimerComponent>();
            Game.AddSingleton<CoroutineLockComponent>();
            Game.AddSingleton<ConfigComponent>().IConfigReader = new ConfigReader();
            Game.AddSingleton<CodeLoaderComponent>().ICodeLoader = new CodeLoader();

            await CodeLoaderComponent.Instance.StartAsync();
            this.m_RunnerComponent = this.gameObject.AddComponent<Runner>();
        }
    }
}