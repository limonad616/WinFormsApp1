using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;
using System.Drawing;

namespace WinFormsApp1
{
    public class NetworkGame(Form1 form, List<Ball> balls, List<TextLine> textLines, int port)
    {
        private UdpClient? udpServer;
        private UdpClient? udpClient;

        public void StartServer()
        {
            udpServer = new UdpClient(port);
            new Thread(() =>
            {
                while (true)
                {
                    if (balls.Count == 0 && textLines.Count == 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    string state = "STATE:";
                    foreach (var ball in balls)
                    {
                        string serializedBall = ball.Serialize();
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
                        udpServer.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
                    }
                    catch (Exception)
                    {
                    }

                    if (udpServer.Available > 0)
                    {
                        IPEndPoint remoteEndPoint = new(IPAddress.Any, 0);
                        byte[] receiveData = udpServer.Receive(ref remoteEndPoint);
                        string message = Encoding.UTF8.GetString(receiveData);
                        if (message.StartsWith("TEXTADD:", StringComparison.Ordinal))
                        {
                            var textData = message[8..].Split('|');
                            if (textData.Length == 4)
                            {
                                form.Invoke((MethodInvoker)delegate
                                {
                                    textLines.Add(new TextLine(
                                        textData[0],
                                        new PointF(float.Parse(textData[1], CultureInfo.InvariantCulture),
                                                   float.Parse(textData[2], CultureInfo.InvariantCulture)),
                                        float.Parse(textData[3], CultureInfo.InvariantCulture)));
                                    form.Invalidate();
                                });
                            }
                        }
                        else if (message.StartsWith("CONNECT:", StringComparison.Ordinal))
                        {
                            form.Invoke((MethodInvoker)delegate
                            {
                                textLines.Add(new TextLine("Подключился пользователь", new PointF(50, 50), 12));
                                form.Invalidate();
                            });
                        }
                    }
                    Thread.Sleep(100);
                }
            })
            { IsBackground = true }.Start();
        }

        public void StartClient()
        {
            udpClient = new UdpClient(port);
            byte[] connectData = Encoding.UTF8.GetBytes("CONNECT:");
            udpClient.Send(connectData, connectData.Length, new IPEndPoint(IPAddress.Broadcast, port));

            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        IPEndPoint remoteEndPoint = new(IPAddress.Any, 0);
                        byte[] data = udpClient.Receive(ref remoteEndPoint);
                        string state = Encoding.UTF8.GetString(data);
                        if (state.StartsWith("STATE:", StringComparison.Ordinal))
                        {
                            UpdateState(state[6..]);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            })
            { IsBackground = true }.Start();
        }

        public void SendText(string text, float x, float y, float fontSize)
        {
            if (udpClient != null)
            {
                string newText = $"TEXTADD:{text}|{x.ToString(CultureInfo.InvariantCulture)}|{y.ToString(CultureInfo.InvariantCulture)}|{fontSize.ToString(CultureInfo.InvariantCulture)}";
                byte[] data = Encoding.UTF8.GetBytes(newText);
                try
                {
                    udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
                }
                catch (Exception)
                {
                }
            }
        }

        private void UpdateState(string stateData)
        {
            if (form.InvokeRequired)
            {
                form.Invoke((MethodInvoker)delegate { UpdateState(stateData); });
                return;
            }

            try
            {
                balls.Clear();
                textLines.Clear();
                var parts = stateData.Split(';');
                if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
                {
                    return;
                }

                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part)) continue;
                    if (part.StartsWith("BALL:", StringComparison.Ordinal))
                    {
                        var ballData = part[5..].Split(',');
                        if (ballData.Length == 5)
                        {
                            try
                            {
                                float x = float.Parse(ballData[0], CultureInfo.InvariantCulture);
                                float y = float.Parse(ballData[1], CultureInfo.InvariantCulture);
                                float velX = float.Parse(ballData[2], CultureInfo.InvariantCulture);
                                float velY = float.Parse(ballData[3], CultureInfo.InvariantCulture);
                                float radius = float.Parse(ballData[4], CultureInfo.InvariantCulture);
                                balls.Add(new Ball(x, y, radius, new Random()) { Velocity = new PointF(velX, velY) });
                            }
                            catch (FormatException)
                            {
                            }
                        }
                    }
                    else if (part.StartsWith("TEXT:", StringComparison.Ordinal))
                    {
                        var textData = part[5..].Split('|');
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
                    }
                }
            }
            catch (Exception)
            {
            }
            form.Invalidate();
        }
    }
}