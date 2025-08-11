using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionControl.Demo.Drivers;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotionControl.Demo.ViewModels
{
    // VM：UI 状态/参数/命令，调度 轨迹规划 + 适配器执行
    public partial class MainViewModel : ObservableObject
    {
        private IMotionCard _card;// 当前使用的卡
        private CancellationTokenSource? _ctsExec;// 规划执行的取消令牌

        // 绑定到 UI 的属性
        [ObservableProperty] private string selectedAdapter = "MockCard";
        [ObservableProperty] private string status = "未连接";
        [ObservableProperty] private string connectButtonText = "连接";
        [ObservableProperty] private double position;  // 实时位置显示
        [ObservableProperty] private double jogVelocity = 10;// Jog 速度
        [ObservableProperty] private double targetPosition = 100;
        [ObservableProperty] private double vmax = 80;
        [ObservableProperty] private double acc = 200;
        [ObservableProperty] private double dec = 200;
        [ObservableProperty] private int sampleMs = 10;// 采样周期（ms）
        [ObservableProperty] private string planSummary = ""; // 规划摘要文本
        [ObservableProperty] private double softLimitMin = -500;
        [ObservableProperty] private double softLimitMax = 500;
        [ObservableProperty] private string axisStatus = "Axis=0; ServoOff, NotHomed";

        public MainViewModel() { _card = new MockMotionCard(); }

        // 根据下拉框选择切换卡类型
        private void ResolveAdapter()
        {
            _card = SelectedAdapter?.StartsWith("Native") == true ? new NativeMotionCard() : new MockMotionCard();
        }

        // 连接/断开
        [RelayCommand]
        private async Task ToggleConnect()
        {
            ResolveAdapter();
            if (!await _card.IsOpenAsync())
            {
                await _card.OpenAsync();
                await RefreshState();
                Status = "已连接";
                ConnectButtonText = "断开";
            }
            else
            {
                await _card.CloseAsync();
                Status = "未连接";
                ConnectButtonText = "连接";
            }
        }

        // 伺服/回零
        [RelayCommand] private async Task ServoOn() { await _card.ServoOnAsync(0); await RefreshState(); }
        [RelayCommand] private async Task ServoOff() { await _card.ServoOffAsync(0); await RefreshState(); }
        [RelayCommand] private async Task Home() { await _card.HomeAsync(0); await RefreshState(); }

        [RelayCommand] private Task JogPositive() => _card.JogAsync(0, Math.Abs(JogVelocity));
        [RelayCommand] private Task JogNegative() => _card.JogAsync(0, -Math.Abs(JogVelocity));
        [RelayCommand] private Task JogStop() => _card.JogStopAsync(0);

        // 规划 + 执行
        [RelayCommand]
        private async Task PlanAndExecute()
        {
            if (!await _card.IsOpenAsync()) { Status = "未连接"; return; }

            // 1) 获取起点、计算 dt（秒）、生成位置序列
            var cur = await _card.GetPositionAsync(0);
            var dt = Math.Max(1, SampleMs) / 1000.0;
            var points = Services.Trajectory.GenerateTrapezoid(cur, TargetPosition, Vmax, Acc, Dec, dt);

            // 2) 显示计划摘要
            var sb = new StringBuilder();
            sb.AppendLine($"Start={cur:F3}, Target={TargetPosition:F3}, Steps={points.Count}, dt={dt:F3}s");
            sb.AppendLine($"Vmax={Vmax}, Acc={Acc}, Dec={Dec}");
            PlanSummary = sb.ToString();

            // 3) 启动执行：流式发送到卡，并将 Position 绑定到当前执行点
            _ctsExec?.Cancel();
            _ctsExec = new CancellationTokenSource();
            var token = _ctsExec.Token;
            try
            {
                await foreach (var p in _card.StreamPositionsAsync(0, points, dt, token))
                    Position = p;// UI 实时更新位置
            }
            catch (OperationCanceledException) {/* 用户点了停止/急停 */ }
            finally { await RefreshState(); }// 收尾刷新状态文本
        }

        // 停止/急停
        [RelayCommand] private Task Stop() => _card.StopAsync(0);
        [RelayCommand] private Task Estop() => _card.EstopAsync(0);

        // 软限位
        [RelayCommand]
        private async Task ApplySoftLimit()
        {
            _card.ApplySoftLimit(0, SoftLimitMin, SoftLimitMax);
            await RefreshState();
        }

        // 刷新位置与状态展示
        private async Task RefreshState()
        {
            Position = await _card.GetPositionAsync(0);
            var st = await _card.GetAxisStateAsync(0);
            AxisStatus = $"Servo={(st.ServoOn ? "On" : "Off")}, Homed={(st.Homed ? "Yes" : "No")}, Busy={(st.Busy ? "Yes" : "No")}, Alarm={(st.Alarm ? "Yes" : "No")}, Pos={st.Position:F3}";
        }
    }
}
