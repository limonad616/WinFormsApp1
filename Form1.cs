using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly List<Ball> balls;
        private readonly List<TextLine> textLines;
        private readonly Random random;
        private readonly TextBox inputTextBox;
        private readonly NumericUpDown fontSizeSelector;
        private readonly Button addTextButton;
        private readonly System.Windows.Forms.Timer timer;
        private UdpClient? udpServer;
        private UdpClient? udpClient;
        private bool isServer;
        private const int Port = 8888;

        public Form1()
        {
            balls = new List<Ball>();
            textLines = new List<TextLine>();
            random = new Random();
            inputTextBox = new TextBox();
            fontSizeSelector = new NumericUpDown();
            addTextButton = new Button();
            timer = new System.Windows.Forms.Timer();

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
                StartServer();
            };
            clientButton.Click += (s, e) =>
            {
                isServer = false;
                modeForm.Close();
                InitializeComponents();
                StartClient();
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
                for (int i = 0; i < 5; i++)
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

        private void StartServer()
        {
            udpServer = new UdpClient(Port);
            new Thread(() =>
            {
                while (true)
                {
                    if (balls.Count == 0 && textLines.Count == 0)
                    {
                        Console.WriteLine("Warning: No balls or text to send.");
                        Thread.Sleep(100);
                        continue;
                    }

                    string state = "STATE:";
                    foreach (var ball in balls)
                    {
                        string serializedBall = ball.Serialize();
                        Console.WriteLine($"Serializing ball: {serializedBall}"); // Debug output
                        state += $"BALL:{serializedBall};";
                    }
                    foreach (var text in textLines)
                    {
                        string textEntry = $"TEXT:{text.Text}|{text.Position.X.ToString(CultureInfo.InvariantCulture)}|{text.Position.Y.ToString(CultureInfo.InvariantCulture)}|{text.FontSize.ToString(CultureInfo.InvariantCulture)}|";
                        foreach (var color in text.charColors)
                        {
                            textEntry += $"{color.R},{color.G},{color.B},";
                        }
                        textEntry = textEntry.TrimEnd(',') + ";";
                        state += textEntry;
                    }
                    byte[] data = Encoding.UTF8.GetBytes(state);
                    try
                    {
                        udpServer.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, Port));
                        Console.WriteLine($"Server sent state: {state.Substring(0, Math.Min(100, state.Length))}...");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Send error: {ex.Message}");
                    }

                    if (udpServer.Available > 0)
                    {
                        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] receiveData = udpServer.Receive(ref remoteEndPoint);
                        string message = Encoding.UTF8.GetString(receiveData);
                        Console.WriteLine($"Received message: {message}");
                        if (message.StartsWith("TEXTADD:"))
                        {
                            var textData = message.Substring(8).Split('|');
                            if (textData.Length == 4)
                            {
                                Invoke((MethodInvoker)delegate
                                {
                                    textLines.Add(new TextLine(
                                        textData[0],
                                        new PointF(float.Parse(textData[1], CultureInfo.InvariantCulture),
                                                   float.Parse(textData[2], CultureInfo.InvariantCulture)),
                                        float.Parse(textData[3], CultureInfo.InvariantCulture)));
                                    this.Invalidate();
                                });
                            }
                        }
                        else if (message.StartsWith("CONNECT:"))
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                textLines.Add(new TextLine("Подключился пользователь", new PointF(50, 50), 12));
                                this.Invalidate();
                            });
                        }
                    }
                    Thread.Sleep(100);
                }
            })
            { IsBackground = true }.Start();
        }

        private void StartClient()
        {
            udpClient = new UdpClient(Port);
            byte[] connectData = Encoding.UTF8.GetBytes("CONNECT:");
            udpClient.Send(connectData, connectData.Length, new IPEndPoint(IPAddress.Broadcast, Port));
            Console.WriteLine("Client sent connect message");

            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = udpClient.Receive(ref remoteEndPoint);
                        string state = Encoding.UTF8.GetString(data);
                        Console.WriteLine($"Client received: {state.Substring(0, Math.Min(100, state.Length))}...");
                        if (state.StartsWith("STATE:"))
                        {
                            UpdateState(state.Substring(6));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Receive error: {ex.Message}");
                    }
                }
            })
            { IsBackground = true }.Start();
        }

        private void UpdateState(string stateData)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { UpdateState(stateData); });
                return;
            }

            try
            {
                balls.Clear();
                textLines.Clear();
                var parts = stateData.Split(';');
                if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
                {
                    Console.WriteLine("Warning: Empty state data received.");
                    return;
                }

                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part)) continue;
                    if (part.StartsWith("BALL:"))
                    {
                        var ballData = part.Substring(5).Split(',');
                        if (ballData.Length == 5)
                        {
                            try
                            {
                                float x = float.Parse(ballData[0], CultureInfo.InvariantCulture);
                                float y = float.Parse(ballData[1], CultureInfo.InvariantCulture);
                                float velX = float.Parse(ballData[2], CultureInfo.InvariantCulture);
                                float velY = float.Parse(ballData[3], CultureInfo.InvariantCulture);
                                float radius = float.Parse(ballData[4], CultureInfo.InvariantCulture);
                                balls.Add(new Ball(x, y, radius, random) { Velocity = new PointF(velX, velY) });
                                Console.WriteLine($"Added ball at {x}, {y}");
                            }
                            catch (FormatException ex)
                            {
                                Console.WriteLine($"Invalid ball data format: {part}, Error: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Invalid ball data: {part}");
                        }
                    }
                    else if (part.StartsWith("TEXT:"))
                    {
                        var textData = part.Substring(5).Split('|');
                        if (textData.Length >= 4)
                        {
                            var text = textData[0];
                            var x = float.Parse(textData[1], CultureInfo.InvariantCulture);
                            var y = float.Parse(textData[2], CultureInfo.InvariantCulture);
                            var size = float.Parse(textData[3], CultureInfo.InvariantCulture);
                            var textLine = new TextLine(text, new PointF(x, y), size);
                            if (textData.Length > 4)
                            {
                                var colorData = textData[4].Split(',');
                                for (int i = 0; i < text.Length && i * 3 < colorData.Length - 2; i++)
                                {
                                    int r = int.Parse(colorData[i * 3], CultureInfo.InvariantCulture);
                                    int g = int.Parse(colorData[i * 3 + 1], CultureInfo.InvariantCulture);
                                    int b = int.Parse(colorData[i * 3 + 2], CultureInfo.InvariantCulture);
                                    textLine.charColors[i] = Color.FromArgb(r, g, b);
                                }
                            }
                            textLines.Add(textLine);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid text data: {part}");
                        }
                    }
                }
                Console.WriteLine($"Updated state: {balls.Count} balls, {textLines.Count} texts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateState error: {ex.Message}");
            }
            this.Invalidate();
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
                else if (udpClient != null)
                {
                    string newText = $"TEXTADD:{inputTextBox.Text}|{random.Next(50, this.ClientSize.Width - 50).ToString(CultureInfo.InvariantCulture)}|{random.Next(50, this.ClientSize.Height - 50).ToString(CultureInfo.InvariantCulture)}|{fontSizeSelector.Value.ToString(CultureInfo.InvariantCulture)}";
                    byte[] data = Encoding.UTF8.GetBytes(newText);
                    try
                    {
                        udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, Port));
                        Console.WriteLine($"Client sent text: {newText}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Send text error: {ex.Message}");
                    }
                }
                inputTextBox.Clear();
                this.Invalidate();
            }
        }
    }

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
            charColors = new List<Color>();
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
            using (Font font = new Font("Arial", fontSize))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    using (Brush brush = new SolidBrush(charColors[i]))
                    {
                        g.DrawString(text[i].ToString(), font, brush, x, position.Y);
                        x += g.MeasureString(text[i].ToString(), font).Width;
                    }
                }
            }
        }

        public void CheckCollision(Ball ball, Random random)
        {
            using (Bitmap bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font font = new Font("Arial", fontSize))
            {
                float x = position.X;
                for (int i = 0; i < text.Length; i++)
                {
                    SizeF charSize = g.MeasureString(text[i].ToString(), font);
                    RectangleF charRect = new RectangleF(x, position.Y, charSize.Width, charSize.Height);
                    RectangleF ballRect = new RectangleF(
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
}