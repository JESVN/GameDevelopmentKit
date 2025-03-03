﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET
{
    public class CodeLoader : ICodeLoader
    {
        private AssemblyLoadContext assemblyLoadContext;

        private Assembly model;

        public async UniTask StartAsync()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly ass in assemblies)
            {
                if (ass.GetName().Name == "Model")
                {
                    model = ass;
                    break;
                }
            }

            await LoadHotfixAsync();

            IStaticMethod start = new StaticMethod(model, "ET.Entry", "Start");
            start.Run();
        }

        public async UniTask LoadHotfixAsync()
        {
            await UniTask.CompletedTask;
            byte[] dllBytes = File.ReadAllBytes("./Hotfix.dll");
            byte[] pdbBytes = File.ReadAllBytes("./Hotfix.pdb");

            assemblyLoadContext?.Unload();
            GC.Collect();
            assemblyLoadContext = new AssemblyLoadContext("Hotfix", true);
            Assembly hotfix = assemblyLoadContext.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));

            Dictionary<string, Type> types = AssemblyHelper.GetAssemblyTypes(Assembly.GetEntryAssembly(), typeof (Init).Assembly, typeof (Game).Assembly, model, hotfix);

            EventSystem.Instance.Add(types);
        }
    }
}