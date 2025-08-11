namespace MotionControl.Demo.Models
{
    public class AxisState
    {
        public bool ServoOn { get; set; }
        public bool Homed { get; set; }
        public bool Busy { get; set; }
        public bool Alarm { get; set; }
        public double Position { get; set; }// 当前位置
        public double Velocity { get; set; }// 当前速度
        public double SoftMin { get; set; } = -1000;// 软限位下限
        public double SoftMax { get; set; } = 1000;// 软限位上限
    }
}
