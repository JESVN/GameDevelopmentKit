using System;
using System.Collections;
using System.Diagnostics;

namespace ET.Server
{
    public static partial class WatcherHelper
    {
        public static DRStartMachineConfig GetThisMachineConfig()
        {
            string[] localIP = NetworkHelper.GetAddressIPs();
            DRStartMachineConfig startMachineConfig = null;
            foreach (DRStartMachineConfig config in Tables.Instance.DTStartMachineConfig.DataList)
            {
                if (!WatcherHelper.IsThisMachine(config.InnerIP, localIP))
                {
                    continue;
                }
                startMachineConfig = config;
                break;
            }

            if (startMachineConfig == null)
            {
                throw new Exception("not found this machine ip config!");
            }

            return startMachineConfig;
        }
        
        public static bool IsThisMachine(string ip, string[] localIPs)
        {
            if (ip != "127.0.0.1" && ip != "0.0.0.0" && !((IList) localIPs).Contains(ip))
            {
                return false;
            }
            return true;
        }
        
        public static Process StartProcess(int processId, int createScenes = 0)
        {
            DRStartProcessConfig startProcessConfig = Tables.Instance.DTStartProcessConfig.Get(Options.Instance.StartConfig, processId);
            const string exe = "dotnet";
            string arguments = $"App.dll" + 
                    $" --Process={startProcessConfig.Id}" +
                    $" --AppType=Server" +  
                    $" --StartConfig={Options.Instance.StartConfig}" +
                    $" --Develop={Options.Instance.Develop}" +
                    $" --CreateScenes={createScenes}" +
                    $" --LogLevel={Options.Instance.LogLevel}" +
                    $" --Console={Options.Instance.Console}";
            Log.Debug($"{exe} {arguments}");
            Process process = ProcessHelper.Run(exe, arguments);
            return process;
        }
    }
}