using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;


namespace ExifEditor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Process process;

        private void selfolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "请选择需要处理的图片的文件夹"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedPath = dialog.FileName;
                pathfolder.Text = selectedPath;
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            if(!File.Exists(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "exiftool.exe")))
            {
                MessageBox.Show("exiftool.exe不存在，请将其放在本软件的根目录！","缺少ExifTool", 0, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            if(Config.Read("title") != "")
            {
                title.Text = Config.Read("title");
            }
            if(Config.Read("soft") != "")
            {
                soft.Text = Config.Read("soft");
            }
            if(Config.Read("des") != "")
            {
                des.Text = Config.Read("des");
            }
            if(Config.Read("comment") != "")
            {
                comment.Text = Config.Read("comment");
            }
            if(Config.Read("path") != "")
            {
                pathfolder.Text = Config.Read("path");
            }
        }

        private string ConvertToHtmlEntities(string input)
        {
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                // 仅转换非 ASCII 字符（Unicode 码点 > 127）
                if (c > 127)
                {
                    sb.AppendFormat("&#{0};", (int)c);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void start_Click(object sender, RoutedEventArgs e_btn)
        {
            if (!Directory.Exists(pathfolder.Text))
            {
                MessageBox.Show("处理的图片的文件夹不存在！", "错误", 0, MessageBoxImage.Error);
                return;
            }

            start.IsEnabled = false;
            Dispatcher.Invoke(() => { logs.Text = "开始处理···"; });
            save();

            // 转换所有中文参数为 HTML 字符
            string htmlTitle = ConvertToHtmlEntities(title.Text);
            string htmlSoft = ConvertToHtmlEntities(soft.Text);
            string htmlDes = ConvertToHtmlEntities(des.Text);
            string htmlComment = ConvertToHtmlEntities(comment.Text);

            process = new Process();
            process.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "exiftool.exe");
            process.StartInfo.Arguments = $"-charset UTF8 -ext jpg -ext png -overwrite_original -E " +
                                           $"-Title=\"{htmlTitle}\" " +
                                           $"-Software=\"{htmlSoft}\" " +
                                           $"-Description=\"{htmlDes}\" " +
                                           $"-Comment=\"{htmlComment}\" " +
                                           $"\"{pathfolder.Text}\"";

            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            process.StartInfo.EnvironmentVariables["PERL_UNICODE"] = "SD";

            process.OutputDataReceived += (_, e) =>
            {
                Dispatcher.Invoke(() => {
                    string log = $"\n{e.Data?.Replace("directories scanned", "个文件夹被扫描了").Replace("image files updated", "个图片文件信息被成功更新！") ?? ""}";
                    if(log != "\n") { 
                    logs.Text += $"\n[ExifTool]{e.Data?.Replace("directories scanned", "个文件夹被扫描了").Replace("image files updated", "个图片文件信息被成功更新！") ?? ""}";
                    }
                });
            };

            process.ErrorDataReceived += (_, e) =>
            {
                Dispatcher.Invoke(() => {
                    string log = $"\n{e.Data?.Replace("directories scanned", "个文件夹被扫描了").Replace("image files updated", "个图片文件信息被成功更新！") ?? ""}";
                    if (log != "\n")
                    {
                        logs.Text += $"\n[ExifTool]{e.Data?.Replace("directories scanned", "个文件夹被扫描了").Replace("image files updated", "个图片文件信息被成功更新！") ?? ""}";
                    }
                });
            };

            process.Exited += (_, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    logs.Text += "\n处理成功!";
                    start.IsEnabled = true;
                });
            };

            process.EnableRaisingEvents = true;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }



        private void save()
        {
            Config.Write("title", title.Text);
            Config.Write("soft", soft.Text);
            Config.Write("des", des.Text);
            Config.Write("comment", comment.Text);
            Config.Write("path", pathfolder.Text);
        }
    }
}
