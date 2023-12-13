using PingaTor.Methods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace PingaTor.Models
{
    internal class Run
    {
        string pathAction;
        string pathRequest;

        SMTelegram.Notification notification;

        Dictionary<string, DateTime> lastFileUpdate = new();
        Dictionary<string, Request> requests = new();

        System.Timers.Timer timer;

        public Run(string pathSetting = "Setting", string pathLog = "Logs", string pathAction = "Actions", string pathRequest = "Requests")
        {
            if (!Directory.Exists(pathLog))
                Directory.CreateDirectory(pathLog);

            if (!Directory.Exists(pathAction))
                Directory.CreateDirectory(pathAction);
            this.pathAction = pathAction;

            if (!Directory.Exists(pathRequest))
                Directory.CreateDirectory(pathRequest);
            this.pathRequest = pathRequest;

            Utils.Set(pathLog);

            if (!File.Exists(pathSetting + ".json"))
            {
                Utils.WriteLine("Setting.json not found");
                Utils.Exit();
            }

            Setting setting;
            using (var file = File.OpenRead(pathSetting + ".json"))
            {
                setting = JsonSerializer.Deserialize<Setting>(file);
            }

            notification = new SMTelegram.Notification(setting.TokenBot, string.Empty, setting.DevOpsChat, string.Empty);

            Utils.Write(Environment.NewLine + Environment.NewLine + $"{DateTime.Now.ToLongTimeString()} Start programm" + Environment.NewLine);

            SetRequestSettings();
            timer = new System.Timers.Timer(10000);
            timer.Elapsed += SetRequestSettings;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        void SetRequestSettings(object source, ElapsedEventArgs e)
        {
            SetRequestSettings();
        }

        void SetRequestSettings()
        {
            string[] allPaths = Directory.GetFiles(pathRequest, "*.json");

            if (allPaths.Length == 0)
            {
                Utils.WriteLine("Request folder is empty");
                return;
            }

            foreach (string path in lastFileUpdate.Keys)
            {
                if (!allPaths.Contains(path))
                {
                    lastFileUpdate.Remove(path);

                    requests[path].Finish();

                    requests.Remove(path);
                }
            }

            foreach (string path in allPaths)
            {
                try
                {
                    if (lastFileUpdate.ContainsKey(path))
                    {
                        var lastUpdate = File.GetLastWriteTime(path);

                        if (lastFileUpdate[path] != lastUpdate)
                        {

                            RequestSetting setting;

                            using (var file = File.OpenRead(path))
                            {
                                try
                                {
                                    setting = JsonSerializer.Deserialize<RequestSetting>(file);
                                }
                                catch (JsonException ex)
                                {
                                    Utils.WriteLine($"\"{path}\" invalid json file:{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}");
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    Utils.WriteLine($"Exception when trying to deserialize \"{path}\":{Environment.NewLine}{ex}");
                                    continue;
                                }
                            }

                            var names = path.Split('\\');

                            setting.NameRequest = names[names.Length - 1];

                            lastFileUpdate[path] = lastUpdate;
                            requests[path].Finish();
                            requests[path] = new Request(setting, notification, pathAction);
                        }
                    }
                    else
                    {
                        RequestSetting setting;

                        using (var file = File.OpenRead(path))
                        {
                            try
                            {
                                setting = JsonSerializer.Deserialize<RequestSetting>(file);
                            }
                            catch (JsonException ex)
                            {
                                Utils.WriteLine($"\"{path}\" invalid json file:{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}");
                                continue;
                            }
                            catch (Exception ex)
                            {
                                Utils.WriteLine($"Exception when trying to deserialize \"{path}\":{Environment.NewLine}{ex}");
                                continue;
                            }
                        }

                        var names = path.Split('\\');

                        setting.NameRequest = names[names.Length - 1];

                        lastFileUpdate.Add(path, File.GetLastWriteTime(path));
                        requests.Add(path, new Request(setting, notification, pathAction));
                    }
                }
                catch (Exception ex)
                {
                    Utils.WriteLine($"Exception when trying to read a file \"{path}\":{Environment.NewLine}{ex}");
                }
            }
        }
    }
}
