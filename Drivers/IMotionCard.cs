using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MotionControl.Demo.Models;

namespace MotionControl.Demo.Drivers
{
    public interface IMotionCard
    {
        // 适配器接口
        Task OpenAsync();
        Task CloseAsync();
        Task<bool> IsOpenAsync();

        Task ServoOnAsync(int axis);
        Task ServoOffAsync(int axis);

        Task HomeAsync(int axis);

        Task<double> GetPositionAsync(int axis);
        Task SetPositionAsync(int axis, double pos);

        Task<AxisState> GetAxisStateAsync(int axis);

        Task MoveAbsAsync(int axis, double position, double vmax, double acc, double dec);
        Task MoveRelAsync(int axis, double delta, double vmax, double acc, double dec);

        Task JogAsync(int axis, double velocity);
        Task JogStopAsync(int axis);

        Task StopAsync(int axis); // 减速停
        Task EstopAsync(int axis); // 急停

        // “预规划位置流”执行：dt 为采样周期（秒）
        IAsyncEnumerable<double> StreamPositionsAsync(int axis, IEnumerable<double> positions, double dtSeconds, CancellationToken token);
        
        // 设置软限位（也可在 SDK 内部设置）
        void ApplySoftLimit(int axis, double min, double max);
    }
}
