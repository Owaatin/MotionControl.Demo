using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MotionControl.Demo.Models;

namespace MotionControl.Demo.Drivers
{
    public class NativeMotionCard : IMotionCard
    {
        // 把 TODO 换成厂家 API
        private bool _open;
        private AxisState _axis = new();

        public Task OpenAsync() { /* TODO: 打开/初始化设备 */ _open = true; return Task.CompletedTask; }
        public Task CloseAsync() { /* TODO: 关闭设备 */ _open = false; return Task.CompletedTask; }
        public Task<bool> IsOpenAsync() => Task.FromResult(_open);

        public Task ServoOnAsync(int axis) { /* TODO */ _axis.ServoOn = true; return Task.CompletedTask; }
        public Task ServoOffAsync(int axis) { /* TODO */ _axis.ServoOn = false; return Task.CompletedTask; }
        public Task HomeAsync(int axis) { /* TODO */ _axis.Homed = true; _axis.Position = 0; return Task.CompletedTask; }

        public Task<double> GetPositionAsync(int axis) { /* TODO: 读真实位置 */ return Task.FromResult(_axis.Position); }
        public Task SetPositionAsync(int axis, double pos) { /* TODO */ _axis.Position = pos; return Task.CompletedTask; }
        public Task<AxisState> GetAxisStateAsync(int axis) => Task.FromResult(_axis);

        public Task MoveAbsAsync(int axis, double position, double vmax, double acc, double dec) { /* TODO */ _axis.Position = position; return Task.CompletedTask; }
        public Task MoveRelAsync(int axis, double delta, double vmax, double acc, double dec) { /* TODO */ _axis.Position += delta; return Task.CompletedTask; }

        public Task JogAsync(int axis, double velocity) { /* TODO */ _axis.Velocity = velocity; return Task.CompletedTask; }
        public Task JogStopAsync(int axis) { /* TODO */ _axis.Velocity = 0; return Task.CompletedTask; }

        public Task StopAsync(int axis) { /* TODO: 减速停 */ _axis.Velocity = 0; return Task.CompletedTask; }
        public Task EstopAsync(int axis) { /* TODO: 急停 */ _axis.Velocity = 0; return Task.CompletedTask; }

        public async IAsyncEnumerable<double> StreamPositionsAsync(int axis, IEnumerable<double> positions, double dtSeconds, [EnumeratorCancellation] CancellationToken token)
        {
            // 如果 SDK 支持 PT/PVT/CSV buffer，请改为 “下载缓冲 + 触发执行 + 监听完成”
            foreach (var p in positions)
            {
                if (token.IsCancellationRequested) yield break;
                _axis.Position = p;// TODO: 真卡请读回实际位置
                yield return p;
                await Task.Delay(System.TimeSpan.FromSeconds(dtSeconds), token);
            }
        }

        public void ApplySoftLimit(int axis, double min, double max) { _axis.SoftMin = min; _axis.SoftMax = max; }
    }
}
