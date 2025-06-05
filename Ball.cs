using System.Drawing;
using System.Globalization;

namespace WinFormsApp1
{
    public class Ball
    {
        public PointF Position;
        public PointF Velocity;
        public float Radius { get; set; }
        private Random? random; // Made nullable to satisfy C# nullability rules

        public Ball(float x, float y, float radius, Random? random = null)
        {
            this.Position = new PointF(x, y);
            this.Velocity = new PointF((float)(random?.NextDouble() * 4 - 2 ?? new Random().NextDouble() * 4 - 2),
                                      (float)(random?.NextDouble() * 4 - 2 ?? new Random().NextDouble() * 4 - 2));
            this.Radius = radius;
            this.random = random ?? new Random(); // Initialize if null
        }

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

            if (random?.NextDouble() < 0.05 ?? new Random().NextDouble() < 0.05)
            {
                Velocity = new PointF(
                    (float)(random?.NextDouble() * 4 - 2 ?? new Random().NextDouble() * 4 - 2),
                    (float)(random?.NextDouble() * 4 - 2 ?? new Random().NextDouble() * 4 - 2));
            }
        }

        public void Draw(Graphics g)
        {
            g.FillEllipse(Brushes.Red, Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);
        }

        public string Serialize()
        {
            string data = string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4}",
                Position.X, Position.Y, Velocity.X, Velocity.Y, Radius);
            Console.WriteLine($"Serialized Ball: {data}"); // Debug output
            return data;
        }
    }
}