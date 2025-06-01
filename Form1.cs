using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly List<Ball> balls = new();
        private readonly List<TextLine> textLines = new();
        private readonly Random random = new();
        private readonly TextBox inputTextBox = new();
        private readonly NumericUpDown fontSizeSelector = new();
        private readonly Button addTextButton = new();
        private readonly Timer timer = new();

        public Form1()
        {
            InitializeComponents();
            InitializeGame();
        }

        private void InitializeComponents()
        {
            Size = new Size(800, 600);
            DoubleBuffered = true;

            inputTextBox.Location = new Point(10, 10);
            inputTextBox.Width = 200;

            fontSizeSelector.Location = new Point(220, 10);
            fontSizeSelector.Width = 60;
            fontSizeSelector.Minimum = 8;
            fontSizeSelector.Maximum = 72;
            fontSizeSelector.Value = 12;

            addTextButton.Location = new Point(290, 10);
            addTextButton.Text = "Add Text";
            addTextButton.Width = 100;
            addTextButton.Click += AddTextButton_Click;

            Controls.Add(inputTextBox);
            Controls.Add(fontSizeSelector);
            Controls.Add(addTextButton);

            Paint += Form1_Paint;
            Load += Form1_Load;
        }

        private void InitializeGame()
        {
            for (int i = 0; i < 5; i++)
                balls.Add(CreateRandomBall());

            textLines.Add(new TextLine("Hello, World!", new PointF(100, 100), 12));
            textLines.Add(new TextLine("Bouncing Balls", new PointF(200, 200), 14));
            textLines.Add(new TextLine("C# Game", new PointF(300, 300), 16));
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            timer.Interval = 16;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            foreach (var ball in balls)
                ball.Update(ClientSize, random);

            using Graphics g = CreateGraphics();
            foreach (var textLine in textLines)
                foreach (var ball in balls)
                    textLine.CheckCollision(g, ball, random);

            Invalidate();
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var ball in balls)
            {
                e.Graphics.FillEllipse(Brushes.Red,
                    ball.Position.X - ball.Radius, ball.Position.Y - ball.Radius,
                    ball.Radius * 2, ball.Radius * 2);
            }

            foreach (var textLine in textLines)
                textLine.Draw(e.Graphics);
        }

        private void AddTextButton_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(inputTextBox.Text))
            {
                textLines.Add(CreateRandomTextLine(inputTextBox.Text, (float)fontSizeSelector.Value));
                inputTextBox.Clear();
                Invalidate();
            }
        }

        private Ball CreateRandomBall()
        {
            return new Ball
            {
                Position = new PointF(
                    random.Next(50, ClientSize.Width - 50),
                    random.Next(50, ClientSize.Height - 50)),
                Velocity = new PointF(
                    (float)(random.NextDouble() * 2 - 1),
                    (float)(random.NextDouble() * 2 - 1)),
                Radius = 10,
                Speed = (float)(random.NextDouble() * 2 + 0.5f) // от 0.5 до 2.5
            };
        }

        private TextLine CreateRandomTextLine(string text, float fontSize)
        {
            return new TextLine(
                text,
                new PointF(random.Next(50, ClientSize.Width - 50), random.Next(50, ClientSize.Height - 50)),
                fontSize);
        }
    }

    public class Ball
    {
        public PointF Position;
        public PointF Velocity;
        public float Radius { get; set; }
        public float Speed { get; set; } = 1f;

        public void Update(Size clientSize, Random random)
        {
            Position = new PointF(
                Position.X + Velocity.X * Speed,
                Position.Y + Velocity.Y * Speed);

            if (Position.X - Radius < 0 || Position.X + Radius > clientSize.Width)
            {
                Velocity = new PointF(-Velocity.X, Velocity.Y);
                Position = new PointF(
                    Math.Max(Radius, Math.Min(clientSize.Width - Radius, Position.X)), Position.Y);
            }

            if (Position.Y - Radius < 50 || Position.Y + Radius > clientSize.Height)
            {
                Velocity = new PointF(Velocity.X, -Velocity.Y);
                Position = new PointF(
                    Position.X, Math.Max(Radius + 50, Math.Min(clientSize.Height - Radius, Position.Y)));
            }

            if (random.NextDouble() < 0.05)
            {
                Velocity = new PointF(
                    (float)(random.NextDouble() * 2 - 1),
                    (float)(random.NextDouble() * 2 - 1));
            }
        }
    }

    public class TextLine
    {
        private readonly string text;
        private readonly PointF position;
        private readonly float fontSize;
        private readonly List<Color> charColors;

        public TextLine(string text, PointF position, float fontSize)
        {
            this.text = text;
            this.position = position;
            this.fontSize = fontSize;
            charColors = new List<Color>();
            for (int i = 0; i < text.Length; i++)
                charColors.Add(Color.Black);
        }

        public void Draw(Graphics g)
        {
            float x = position.X;
            using Font font = new("Arial", fontSize);
            for (int i = 0; i < text.Length; i++)
            {
                using Brush brush = new SolidBrush(charColors[i]);
                g.DrawString(text[i].ToString(), font, brush, x, position.Y);
                x += g.MeasureString(text[i].ToString(), font).Width;
            }
        }

        public void CheckCollision(Graphics g, Ball ball, Random random)
        {
            float x = position.X;
            using Font font = new("Arial", fontSize);
            for (int i = 0; i < text.Length; i++)
            {
                SizeF charSize = g.MeasureString(text[i].ToString(), font);
                RectangleF charRect = new(x, position.Y, charSize.Width, charSize.Height);
                RectangleF ballRect = new(ball.Position.X - ball.Radius, ball.Position.Y - ball.Radius, ball.Radius * 2, ball.Radius * 2);

                if (charRect.IntersectsWith(ballRect))
                {
                    charColors[i] = Color.FromArgb(
                        random.Next(256),
                        random.Next(256),
                        random.Next(256));
                }
                x += charSize.Width;
            }
        }
    }
}
