using System;
using System.Collections.Generic;

namespace MotionControl.Demo.Services
{
    public static class Trajectory
    {
        // 生成离散位置序列。根据距离自动选择“三角形”或“梯形”速度曲线。
        public static List<double> GenerateTrapezoid(double x0, double x1, double vmax, double acc, double dec, double dt)
        {
            if (dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));
            // 统一处理正向：d >= 0，记录方向后再恢复
            var d = x1 - x0;
            var dir = Math.Sign(d);
            d = Math.Abs(d);
            vmax = Math.Abs(vmax);
            acc = Math.Abs(acc);
            dec = Math.Abs(dec);
            
            // 加/减速段所需距离
            var da = 0.5 * vmax * vmax / acc;
            var dd = 0.5 * vmax * vmax / dec;

            // 求峰值速度 vpeak 与各段时间 ta/tc/td
            double vpeak, ta, tc, td;
            // 距离不够跑满 vmax，退化为三角形
            if (d < da + dd) { vpeak = Math.Sqrt(d * 2 * acc * dec / (acc + dec)); ta = vpeak / acc; td = vpeak / dec; tc = 0; }
            // 标准梯形
            else { vpeak = vmax; ta = vpeak / acc; td = vpeak / dec; var dc = d - da - dd; tc = dc / vpeak; }

            var positions = new List<double>();
            double T = ta + tc + td;
            int steps = Math.Max(1, (int)Math.Ceiling(T / dt));

            for (int i = 0; i <= steps; i++)
            {
                double t = i * dt, x; // 当前时间// 当前位置
                if (t <= ta) x = 0.5 * acc * t * t;
                else if (t <= ta + tc) { var t2 = t - ta; x = 0.5 * acc * ta * ta + vpeak * t2; }
                else if (t <= T) { var t3 = t - ta - tc; x = 0.5 * acc * ta * ta + vpeak * tc + (vpeak * t3 - 0.5 * dec * t3 * t3); }
                else x = d;// 末点对齐，避免累计误差

                positions.Add(x0 + dir * x);// 恢复方向
            }
            return positions;
        }
    }
}
