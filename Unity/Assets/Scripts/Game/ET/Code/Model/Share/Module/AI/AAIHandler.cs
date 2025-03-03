using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET
{
    public class AIHandlerAttribute: BaseAttribute
    {
    }
    
    [AIHandler]
    public abstract class AAIHandler
    {
        // 检查是否满足条件
        public abstract int Check(AIComponent aiComponent, DRAIConfig aiConfig);

        // 协程编写必须可以取消
        public abstract UniTaskVoid Execute(AIComponent aiComponent, DRAIConfig aiConfig, CancellationTokenSource cts);
    }
}