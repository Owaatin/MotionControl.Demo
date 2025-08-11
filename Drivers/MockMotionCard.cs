using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MotionControl.Demo.Models;

namespace MotionControl.Demo.Drivers
{
    // 用 Task.Delay 按 dt 推进位置
    public class MockMotionCard : IMotionCard
    {
        private readonly AxisState _axis = new();
        private bool _open;
        private CancellationTokenSource? _ctsJog;
        private CancellationTokenSource? _ctsMove;

        public Task OpenAsync() { _open = true; return Task.CompletedTask; }
        public Task CloseAsync() { _open = false; _ctsJog?.Cancel(); _ctsMove?.Cancel(); return Task.CompletedTask; }
        public Task<bool> IsOpenAsync() => Task.FromResult(_open);

        public Task ServoOnAsync(int axis) { _axis.ServoOn = true; return Task.CompletedTask; }
        public Task ServoOffAsync(int axis) { _axis.ServoOn = false; return Task.CompletedTask; }

        public async Task HomeAsync(int axis)
        {
            if (!_axis.ServoOn) throw new InvalidOperationException("Servo OFF");
            _axis.Busy = true;
            await Task.Delay(500);// 模拟回零耗时
            _axis.Position = 0;
            _axis.Homed = true;
            _axis.Busy = false;
        }

        public Task<double> GetPositionAsync(int axis) => Task.FromResult(_axis.Position);
        public Task SetPositionAsync(int axis, double pos) { _axis.Position = pos; return Task.CompletedTask; }
        public Task<AxisState> GetAxisStateAsync(int axis) => Task.FromResult(new AxisState
        {
            ServoOn = _axis.ServoOn,
            Homed = _axis.Homed,
            Busy = _axis.Busy,
            Alarm = _axis.Alarm,
            Position = _axis.Position,
            Velocity = _axis.Velocity,
            SoftMin = _axis.SoftMin,
            SoftMax = _axis.SoftMax
        });

        public async Task MoveAbsAsync(int axis, double position, double vmax, double acc, double dec)
        {
            if (!_axis.ServoOn) throw new InvalidOperationException("Servo OFF");
            if (position < _axis.SoftMin || position > _axis.SoftMax) throw new InvalidOperationException("Soft limit");
            _ctsMove?.Cancel();
            _ctsMove = new CancellationTokenSource();
            _axis.Busy = true;

            // 用轨迹规划生成离散点，用 10ms 推进
            double dt = 0.01;
            var start = _axis.Position;
            var traj = Services.Trajectory.GenerateTrapezoid(start, position, vmax, acc, dec, dt);
            foreach (var p in traj)
            {
                _axis.Position = p;
                if (_ctsMove.IsCancellationRequested) break;
                await Task.Delay(TimeSpan.FromSeconds(dt));// 模拟时基
            }
            _axis.Velocity = 0;
            _axis.Busy = false;
        }

        public Task MoveRelAsync(int axis, double delta, double vmax, double acc, double dec)
            => MoveAbsAsync(axis, _axis.Position + delta, vmax, acc, dec);

        public async Task JogAsync(int axis, double velocity)
        {
            if (!_axis.ServoOn) throw new InvalidOperationException("Servo OFF");
            _ctsJog?.Cancel();
            _ctsJog = new CancellationTokenSource();
            var token = _ctsJog.Token;
            _axis.Busy = true;
            // 开一个后台循环，持续按速度推进位置
            _ = Task.Run(async () =>
            {
                double dt = 0.01;
                while (!token.IsCancellationRequested)
                {
                    var next = _axis.Position + velocity * dt;
                    if (next < _axis.SoftMin || next > _axis.SoftMax) break;
                    _axis.Position = next;
                    _axis.Velocity = velocity;
                    await Task.Delay(TimeSpan.FromSeconds(dt), token).ConfigureAwait(false);
                }
                _axis.Velocity = 0;
                _axis.Busy = false;
            });
            await Task.CompletedTask;
        }

        public Task JogStopAsync(int axis) { _ctsJog?.Cancel(); return Task.CompletedTask; }
        public Task StopAsync(int axis) { _ctsMove?.Cancel(); _ctsJog?.Cancel(); _axis.Velocity = 0; _axis.Busy = false; return Task.CompletedTask; }
        public Task EstopAsync(int axis) { _ctsMove?.Cancel(); _ctsJog?.Cancel(); _axis.Velocity = 0; _axis.Busy = false; return Task.CompletedTask; }

        public async IAsyncEnumerable<double> StreamPositionsAsync(int axis, IEnumerable<double> positions, double dtSeconds, [EnumeratorCancellation] CancellationToken token)
        {
            // 逐点下发并返回当前点（真卡改为 PT/PVT buffered 下载）
            foreach (var p in positions)
            {
                if (token.IsCancellationRequested) yield break;
                _axis.Position = p;
                yield return p;// 让上层 UI 能边执行边显示 Position
                await Task.Delay(TimeSpan.FromSeconds(dtSeconds), token);
            }
        }

        public void ApplySoftLimit(int axis, double min, double max) { _axis.SoftMin = min; _axis.SoftMax = max; }
    }
}
