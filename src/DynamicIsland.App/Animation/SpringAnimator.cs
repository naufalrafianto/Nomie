using System;
using System.Collections.Generic;
using System.Text;

namespace DynamicIsland.App.Animation
{
    public sealed class SpringAnimator
    {
        private readonly double _stiffness;
        private readonly double _damping;
        private double _velocity;

        public double Value { get; private set; }
        public double Target { get; set; }

        public bool IsSettled => Math.Abs(Target - Value) < 0.001 && Math.Abs(_velocity) < 0.001;
        
        public SpringAnimator(double initial = 0, double stiffness = 300, double damping = 22)
        {
            Value = initial;
            Target = initial;
            _stiffness = stiffness;
            _damping = damping;
        }

        /// <param name="dt">Selisih waktu sejak frame sebelumnya, dalam detik.</param>
        public void Step(double dt)
        {
            dt = Math.Min(dt, 1.0 / 30);
            double acceleration = - _stiffness * (Value - Target) - _damping * _velocity;
            _velocity += acceleration * dt;
            Value += _velocity * dt;
            
            if (IsSettled)
            {
                Value = Target;
                _velocity = 0;
            }
        }
    }
}
