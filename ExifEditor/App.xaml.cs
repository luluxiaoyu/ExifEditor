using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ExifEditor
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 添加崩溃处理事件
            DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show("程序在运行的时候发生了异常，异常代码：\n" + e.Exception.Message + "\n若软件闪退，请联系作者进行反馈", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // 设置为已处理，阻止应用程序崩溃
            };
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string ConfigPath = Directory.GetCurrentDirectory() + "\\ExifEditor.json";
            if (!File.Exists(ConfigPath))
            {
                using (StreamWriter sw = File.CreateText(ConfigPath))
                {
                    sw.WriteLine("{}");
                }
            }
        }
        }
}
