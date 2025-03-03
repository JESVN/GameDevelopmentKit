using Cysharp.Threading.Tasks;

namespace ET.Server
{
    // 这里为什么能定义class呢？因为这里只有逻辑，热重载后新的handler替换旧的，仍然没有问题
    [EnableClass]
    public abstract class ARobotCase: AInvokeHandler<RobotInvokeArgs, UniTask>
    {
        protected abstract UniTask Run(RobotCase robotCase);

        public override async UniTask Handle(RobotInvokeArgs a)
        {
            using RobotCase robotCase = await RobotCaseComponent.Instance.New();
            
            try
            {
                await this.Run(robotCase);
            }
            catch (System.Exception e)
            {
                Log.Error($"{robotCase.DomainZone()} {e}");
                RobotLog.Console($"RobotCase Error {this.GetType().FullName}:\n\t{e}");
            }
        }
    }
}