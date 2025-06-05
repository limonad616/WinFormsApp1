using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinFormsApp1
{
    public class TextLine
    {
        private readonly string text;
        private readonly PointF position;
        private readonly float fontSize;
        public readonly List<Color> charColors;

        public TextLine(string text, PointF position, float fontSize)
        {
            this.text = text;
            this.position = position;
            this.fontSize = fontSize;
            charColors = [];
            for (int i = 0; i < text.Length; i++)
            {
                charColors.Add(Color.Black);
            }
        }

        public string Text => text;
        public PointF Position => position;
        public float FontSize => fontSize;

        public void Draw(Graphics g)
        {
            float x = position.X;
            using var font = new Font("Arial", fontSize);
            for (int i = 0; i < text.Length; i++)
            {
                using var brush = new SolidBrush(charColors[i]);
                g.DrawString(text[i].ToString(), font, brush, x, position.Y);
                x += g.MeasureString(text[i].ToString(), font).Width;
            }
        }

        public void CheckCollision(Ball ball, Random random)
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            using var font = new Font("Arial", fontSize);
            float x = position.X;
            for (int i = 0; i < text.Length; i++)
            {
                SizeF charSize = g.MeasureString(text[i].ToString(), font);
                RectangleF charRect = new(x, position.Y, charSize.Width, charSize.Height);
                RectangleF ballRect = new(
                    ball.Position.X - ball.Radius,
                    ball.Position.Y - ball.Radius,
                    ball.Radius * 2,
                    ball.Radius * 2);

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