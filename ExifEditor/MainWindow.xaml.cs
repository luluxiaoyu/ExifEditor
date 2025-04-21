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
    public partial class MainWindow 
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
            if (Config.Read("source") != "")
            {
                source.Text = Config.Read("source");
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
            launch(pathfolder.Text);
            Dispatcher.Invoke(() => { logs.Text = "开始处理···"; });

        }

        private void launch(string path,bool isFolder = true)
        {
            save();
            // 转换所有中文参数为 HTML 字符
            string htmlTitle = ConvertToHtmlEntities(title.Text);
            string htmlSoft = ConvertToHtmlEntities(soft.Text);
            string htmlDes = ConvertToHtmlEntities(des.Text);
            string htmlComment = ConvertToHtmlEntities(comment.Text);

            process = new Process();
            process.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "exiftool.exe");
            if (isFolder)
            {
                process.StartInfo.Arguments = $"-charset UTF8 -ext jpg -ext png -overwrite_original -E -all= " +
                                           $"-Title=\"{htmlTitle}\"  -Source=\"{ConvertToHtmlEntities(source.Text)}\" " +
                                           $"-Software=\"{htmlSoft}\" " +
                                           $"-Description=\"{htmlDes}\" " +
                                           $"-Comment=\"{htmlComment}\" " +
                                           $"\"{path}\"";
            }
            else
            {
                process.StartInfo.Arguments = $"-charset UTF8 -ext jpg -ext png -overwrite_original -E -all= " +
                                           $"-Title=\"{htmlTitle}\" -Source=\"{ConvertToHtmlEntities(source.Text)}\" " +
                                           $"-Software=\"{htmlSoft}\" " +
                                           $"-Description=\"{htmlDes}\" " +
                                           $"-Comment=\"{htmlComment}\" " +
                                           $"{path}";
            }

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
                    if (log != "\n")
                    {
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
            Config.Write("source", source.Text);
        }

        private void imgs_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                string allFiles = "";
                logs.Text=($"拖入了 {files.Length} 个文件,开始处理···");

                // 逐个处理文件
                foreach (string filePath in files)
                {
                    if(filePath.EndsWith(".jpg") || filePath.EndsWith(".png"))
                    {
                        allFiles = $"{allFiles} \"{filePath}\"";
                        logs.Text += $"\n[传入文件路径]{filePath}";
                    }
                    else
                    {
                        logs.Text += $"\n[无效图片]文件 {filePath} 不是有效的图片文件，已跳过。";
                    }
                }
                start.IsEnabled = false;
                launch(allFiles,false);
            }
            else
            {
                MessageBox.Show("未检测到有效文件拖放");
            }
            e.Handled = true;
        }

    }
}
