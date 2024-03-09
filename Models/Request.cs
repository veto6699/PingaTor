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
        readonly Notification _notification;

        readonly string _name;
        readonly List<string>? _responseFragments;
        readonly Uri _url;
        readonly HttpMethod _method;
        readonly string? _fileAction;
        readonly HttpStatusCode _statusCode = HttpStatusCode.OK;
        readonly HttpContent? _httpContent;

        Exception _exception;

        readonly HttpClient _client;

        System.Timers.Timer _timer;

        public Request(RequestSetting setting, Notification notification, string pathAction)
        {
            if (setting.Period <= 5)
                throw new ArgumentException("period <= 5");

            try
            {
                _url = new Uri(setting.URL);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"URL not valud {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(setting.Method))
                throw new ArgumentException("Method is empty");
            else
                _method = new(setting.Method);

            if (_method != HttpMethod.Get)
            {
                try
                {
                    _httpContent = new StringContent(setting.Request);
                    _httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Request not valud {ex.Message}");
                }
            }

            if (setting.HTTPCode > 0)
            {
                string code = setting.HTTPCode.ToString();

                if (Enum.IsDefined(typeof(HttpStatusCode), code))
                    _statusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), code);
            }

            if (setting.Timeout != 0)
            {
                SocketsHttpHandler socketsHandler = new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromMilliseconds(setting.Timeout),
                };
                _client = new HttpClient(socketsHandler);
            }
            else
            {
                SocketsHttpHandler socketsHandler = new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromMinutes(5),
                };
                _client = new HttpClient(socketsHandler);
            }

            if (setting.ResponseFragments is not null && setting.ResponseFragments.Count > 0)
            {
                _responseFragments = new();

                foreach (var fragment in setting.ResponseFragments)
                {
                    if (!string.IsNullOrWhiteSpace(fragment))
                        _responseFragments.Add(fragment);
                }
            }

            if (File.Exists($"{pathAction}\\{setting.FileAction}"))
                _fileAction = setting.FileAction;

            _name = setting.NameRequest;

            _notification = notification;

            Check();
            _timer = new System.Timers.Timer(setting.Period);
            _timer.Elapsed += Check;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        void Check(object source, ElapsedEventArgs e)
        {
            Check();
        }

        async void Check()
        {
            StringBuilder text = new();
            text.Append($"{_name}: ");
            bool needTelegramSend = false;
            HttpResponseMessage? response = null;

            HttpRequestMessage _request = new(_method, _url)
            {
                Content = _httpContent
            };

            try
            {
                response = await _client.SendAsync(_request);
            }
            catch (Exception ex)
            {
                needTelegramSend = true;
                _exception = ex;
                text.Append(ex.Message);
            }

            if (response is not null && response.StatusCode != _statusCode)
            {
                text.Append($"Response Status Code = {response.StatusCode}({(int)response.StatusCode})");
                needTelegramSend = true;

                if (_fileAction is not null)
                    Utils.RunFile(_fileAction);
            }

            string? rezult = null;

            if (_exception is null)
                rezult = await response.Content.ReadAsStringAsync();

            if (_responseFragments is not null && rezult is not null)
            {
                foreach (var fragment in _responseFragments)
                {
                    if (!rezult.Contains(fragment))
                    {
                        needTelegramSend = true;
                        if (string.IsNullOrWhiteSpace(rezult))
                            text.Append("пустой ответ");
                        else
                            text.Append(rezult);

                        if (response.StatusCode != _statusCode && _fileAction is not null)
                            Utils.RunFile(_fileAction);

                        break;
                    }
                }
            }
            else if (response is not null && response.StatusCode == _statusCode)
            {
                text.Append("Ok");
            }

            if (needTelegramSend)
            {
                string? requestString = null;

                if (_request.Content is not null)
                    requestString = await _request.Content.ReadAsStringAsync();

                string telegramFile = _request?.RequestUri?.ToString() + Environment.NewLine + Environment.NewLine + "Запрос" + Environment.NewLine + requestString;

                if (!string.IsNullOrEmpty(rezult))
                    telegramFile += Environment.NewLine + Environment.NewLine + "Ответ" + Environment.NewLine + rezult;
                else if(_exception is not null)
                    telegramFile += Environment.NewLine + Environment.NewLine + "Ответ" + Environment.NewLine + _exception;

                string message;
                if (response is not null && response.StatusCode != _statusCode)
                    message = "Bad request " + (int)response.StatusCode;
                else
                    message = "Bad request reference not equal response";

                await _notification.SendTextAndFile(Notification.Type.Service, Notification.Level.Error, message, telegramFile, "file.txt");
            }

            response?.Dispose();

            Utils.WriteLine(text.ToString());
        }

        public void Finish()
        {
            _timer.Stop();
        }
    }
}
