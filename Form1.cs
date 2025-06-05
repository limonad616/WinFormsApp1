using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly List<Ball> balls = [];
        private readonly List<TextLine> textLines = [];
        private readonly Random random;
        private readonly TextBox inputTextBox;
        private readonly NumericUpDown fontSizeSelector;
        private readonly Button addTextButton;
        private readonly System.Windows.Forms.Timer timer;
        private readonly NetworkGame networkGame;
        private bool isServer;
        private const int Port = 8888;

        public Form1()
        {
            random = new Random();
            inputTextBox = new TextBox();
            fontSizeSelector = new NumericUpDown();
            addTextButton = new Button();
            timer = new System.Windows.Forms.Timer();
            networkGame = new NetworkGame(this, balls, textLines, Port);

            var modeForm = new Form
            {
                Text = "Выберите режим",
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterScreen
            };
            var serverButton = new Button { Text = "Запустить сервер", Location = new Point(50, 20), Size = new Size(200, 30) };
            var clientButton = new Button { Text = "Подключиться как клиент", Location = new Point(50, 60), Size = new Size(200, 30) };
            modeForm.Controls.Add(serverButton);
            modeForm.Controls.Add(clientButton);

            serverButton.Click += (s, e) =>
            {
                isServer = true;
                modeForm.Close();
                InitializeComponents();
                InitializeGame();
                networkGame.StartServer();
            };
            clientButton.Click += (s, e) =>
            {
                isServer = false;
                modeForm.Close();
                InitializeComponents();
                networkGame.StartClient();
            };

            modeForm.ShowDialog();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(800, 600);
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

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

            this.Controls.Add(inputTextBox);
            this.Controls.Add(fontSizeSelector);
            this.Controls.Add(addTextButton);

            this.Paint += Form1_Paint;
            this.Load += Form1_Load;
        }

        private void InitializeGame()
        {
            if (isServer)
            {
                for (int i = 0; i < 10; i++)
                {
                    balls.Add(new Ball(
                        random.Next(50, this.ClientSize.Width - 50),
                        random.Next(50, this.ClientSize.Height - 50),
                        10,
                        random));
                }
                textLines.Add(new TextLine("Hello, World!", new PointF(100, 100), 12));
                textLines.Add(new TextLine("Bouncing Balls", new PointF(200, 200), 14));
                textLines.Add(new TextLine("C# Game", new PointF(300, 300), 16));
            }
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            timer.Interval = 16;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (isServer)
            {
                foreach (var ball in balls)
                {
                    ball.UpdatePosition(this.ClientSize.Width, this.ClientSize.Height);
                }
                foreach (var textLine in textLines)
                {
                    foreach (var ball in balls)
                    {
                        textLine.CheckCollision(ball, random);
                    }
                }
            }
            this.Invalidate();
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var ball in balls)
            {
                ball.Draw(e.Graphics);
            }

            foreach (var textLine in textLines)
            {
                textLine.Draw(e.Graphics);
            }
        }

        private void AddTextButton_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(inputTextBox.Text))
            {
                if (isServer)
                {
                    textLines.Add(new TextLine(
                        inputTextBox.Text,
                        new PointF(random.Next(50, this.ClientSize.Width - 50), random.Next(50, this.ClientSize.Height - 50)),
                        (float)fontSizeSelector.Value));
                }
                else
                {
                    networkGame.SendText(
                        inputTextBox.Text,
                        random.Next(50, this.ClientSize.Width - 50),
                        random.Next(50, this.ClientSize.Height - 50),
                        (float)fontSizeSelector.Value);
                }
                inputTextBox.Clear();
                this.Invalidate();
            }
        }
    }
}