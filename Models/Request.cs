using PingaTor.Methods;
using SMTelegram;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Timers;

namespace PingaTor.Models
{
    internal class Request
    {
        readonly Notification notification;
        readonly string name;
        readonly string reference;
        readonly Uri url;
        readonly HttpContent request;
        readonly string? fileAction;
        readonly ushort statusCode = 200;

        readonly HttpClient client;

        System.Timers.Timer timer;

        public Request(RequestSetting setting, Notification notification, string pathAction)
        {
            if (setting.Period <= 5)
                throw new ArgumentException("period <= 5");

            try
            {
                url = new Uri(setting.URL);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"URL not valud {ex.Message}");
            }

            try
            {
                request = new StringContent(setting.Request);
                request.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Requestnot valud {ex.Message}");
            }

            if (setting.HTTPCode > 0) 
                statusCode = setting.HTTPCode;

            if (setting.Timeout != 0)
            {
                SocketsHttpHandler socketsHandler = new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromMilliseconds(setting.Timeout),
                };
                client = new HttpClient(socketsHandler);
            }
            else
            {
                SocketsHttpHandler socketsHandler = new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromMinutes(5),
                };
                client = new HttpClient(socketsHandler);
            }

            if (!string.IsNullOrWhiteSpace(setting.Reference))
                reference = setting.Reference.Replace(": ", ":");

            if (File.Exists($"{pathAction}\\{setting.FileAction}"))
                fileAction = setting.FileAction;

            name = setting.NameRequest;

            this.notification = notification;

            Check();
            timer = new System.Timers.Timer(setting.Period);
            timer.Elapsed += Check;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        void Check(object source, ElapsedEventArgs e)
        {
            Check();
        }

        async void Check()
        {
            StringBuilder text = new();
            text.Append($"{name}: ");
            bool needTelegramSend = false;

            using (var response = await client.PostAsync(url, request))
            {
                if ((ushort)response.StatusCode != statusCode)
                {
                    text.Append($"Response Status Code = {response.StatusCode}({(int)response.StatusCode})");
                    needTelegramSend = true;

                    if (fileAction is not null)
                        Utils.RunFile(fileAction);
                }

                var rezult = await response.Content.ReadAsStringAsync();

                if (reference is not null && rezult != reference)
                {
                    needTelegramSend = true;
                    if (string.IsNullOrWhiteSpace(rezult))
                        text.Append("пустой ответ");
                    else
                        text.Append(rezult);

                    if ((ushort)response.StatusCode != statusCode && fileAction is not null)
                        Utils.RunFile(fileAction);
                }
                else if ((ushort)response.StatusCode == statusCode)
                {
                    text.Append("Ok");
                }

                if (needTelegramSend)
                {
                    var requestString = await request.ReadAsStringAsync();

                    string telegramFile = url.ToString() + Environment.NewLine + Environment.NewLine + "Запрос" + Environment.NewLine + requestString;

                    if (!string.IsNullOrEmpty(rezult))
                    {
                        telegramFile += Environment.NewLine + Environment.NewLine + "Ответ" + Environment.NewLine + rezult;
                    }

                    string message;
                    if ((ushort)response.StatusCode != statusCode)
                        message = "Bad request " + (int)response.StatusCode;
                    else
                        message = "Bad request reference not equal response";

                    await notification.SendTextAndFile(Notification.Type.Service, Notification.Level.Error, message, telegramFile, "file.txt");

                }
            }

            Utils.WriteLine(text.ToString());
        }

        public void Finish()
        {
            timer.Stop();
        }
    }
}
