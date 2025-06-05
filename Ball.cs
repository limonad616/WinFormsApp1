using System;
using System.Drawing;
using System.Globalization;

namespace WinFormsApp1
{
    public class Ball(float x, float y, float radius, Random random)
    {
        public PointF Position = new(x, y);
        public PointF Velocity = new((float)(random.NextDouble() * 4 - 2), (float)(random.NextDouble() * 4 - 2));
        public float Radius { get; set; } = radius;
        private readonly Random random = random;

        public void UpdatePosition(int screenWidth, int screenHeight)
        {
            Position.X += Velocity.X;
            Position.Y += Velocity.Y;

            if (Position.X - Radius < 0 || Position.X + Radius > screenWidth)
            {
                Velocity.X = -Velocity.X;
                Position.X = Math.Max(Radius, Math.Min(screenWidth - Radius, Position.X));
            }
            if (Position.Y - Radius < 50 || Position.Y + Radius > screenHeight)
            {
                Velocity.Y = -Velocity.Y;
                Position.Y = Math.Max(Radius + 50, Math.Min(screenHeight - Radius, Position.Y));
            }

            if (random.NextDouble() < 0.05)
            {
                Velocity = new(
                    (float)(random.NextDouble() * 4 - 2),
                    (float)(random.NextDouble() * 4 - 2));
            }
        }

        public void Draw(Graphics g)
        {
            g.FillEllipse(Brushes.Red, Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);
        }

        public string Serialize()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4}",
                Position.X, Position.Y, Velocity.X, Velocity.Y, Radius);
        }
    }
}