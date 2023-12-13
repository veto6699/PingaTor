// See https://aka.ms/new-console-template for more information

using PingaTor.Methods;
using PingaTor.Models;

AutoResetEvent waitHandle = new AutoResetEvent(false);

_ = new Run();

waitHandle.WaitOne();


Utils.Exit();