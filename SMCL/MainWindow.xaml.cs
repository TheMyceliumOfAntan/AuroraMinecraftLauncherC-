using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using CZGL.SystemInfo;

namespace SMCL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainNavView.SelectedItem = Home;
            Navi_download.SelectedItem = Automatic_installation;
            MainTabControl.SelectedIndex = 0;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] javaPaths = new string[0];

            if (ToggleSwitch_AutoSetMemory.IsOn == true)
            {
                MemoryValue MemoryData = CZGL.SystemInfo.WindowsMemory.GetMemory();
                ulong num =  MemoryData.TotalPhysicalMemory - MemoryData.UsedPhysicalMemory ;
                double num2 = (double)num / 1024 / 1024;
                double memory = System.Math.Round(num2);
                TextBox_Memory.Text = (memory.ToString());
            }

            if(File.Exists(System.Environment.CurrentDirectory + @"\SMCL\Config\JavaPath.Json") ==false) 
            {
                javaPaths = await FindAllJavaPathsAsync();
                foreach (string javaPath in javaPaths)
                {
                    this.ComboBox_Java.Items.Add(javaPath);
                }
                Debug.WriteLine("Java Search Complete! " + javaPaths.Length.ToString() + " files in total");
                WriteJavaPathToJson(javaPaths);
            }
            else
            {
                javaPaths = ReadJavaPathsFromJson();
                foreach (string javaPath in javaPaths)
                {
                    this.ComboBox_Java.Items.Add(javaPath);
                }
                Debug.WriteLine("Java Read Complete! " + javaPaths.Length.ToString() + " files in total");
            }
        }



        private void WriteJavaPathToJson(string[] JavaPath)
        {
            string jsonString = JsonConvert.SerializeObject(JavaPath,Formatting.Indented);
            System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\SMCL");
            System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\SMCL\Config");
            File.WriteAllText(System.Environment.CurrentDirectory + @"\SMCL\Config\JavaPath.Json", jsonString);
        }
        static string[] ReadJavaPathsFromJson()
        {
            string jsonString = File.ReadAllText(System.Environment.CurrentDirectory + @"\SMCL\Config\JavaPath.Json");
            string[]? javaPaths = JsonConvert.DeserializeObject<string[]>(jsonString);
            if (javaPaths == null)
            {
                return new string[0]; // 返回空的字符串数组
            }
            else
            {
                return javaPaths;
            }
        }


        static async Task<string[]> FindAllJavaPathsAsync()
        {
            string[] drives = Environment.GetLogicalDrives();
            var javaPaths = new ConcurrentBag<string>();

            await Task.Run(() =>
            {
                Parallel.ForEach(drives, drive =>
                {
                    string[] javaPathsInDrive = FindJavaPathsInDrive(drive);
                    foreach (string javaPath in javaPathsInDrive)
                    {
                        javaPaths.Add(javaPath);
                    }
                });
            });

            return javaPaths.ToArray();
        }

        static string[] FindJavaPathsInDrive(string drive)
        {
            var javaPaths = new List<string>();

            string searchPattern = "javaw.exe";
            string[] directories;

            try
            {
                directories = Directory.GetDirectories(drive);
            }
            catch
            {
                return javaPaths.ToArray();
            }

            Parallel.ForEach(directories, directory =>
            {
                if (IsSystemOrCacheDirectory(directory))
                {
                    return; // 跳过系统目录和缓存目录
                }

                try
                {
                    string[] files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        javaPaths.Add(file);
                        Debug.WriteLine("Find Java:" + file);
                    }
                }
                catch
                {
                    // Ignore any exceptions and continue searching in other directories
                }
            });

            return javaPaths.ToArray();
        }

        static bool IsSystemOrCacheDirectory(string directory)
        {
            // 判断目录是否为系统目录或缓存目录
            string[] systemDirectories = { "Windows", "Program Files", "Program Files (x86)", "System32", "SysWOW64" };
            string[] cacheDirectories = { "Temp", "Cache" };

            string directoryName = System.IO.Path.GetFileName(directory);
            if (systemDirectories.Contains(directoryName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            string? parentDirectoryName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(directory));
            if (parentDirectoryName != null)
            {
                if (cacheDirectories.Contains(parentDirectoryName, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }
            else
            {
                parentDirectoryName = "";
                if (cacheDirectories.Contains(parentDirectoryName, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }
        }

        private void MainNavView_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (MainNavView.SelectedItem == Home)
            {
                MainTabControl.SelectedIndex = 0;
            }
            if (MainNavView.SelectedItem == Account)
            {
                MainTabControl.SelectedIndex = 1;
            }
            if (MainNavView.SelectedItem == Download)
            {
                MainTabControl.SelectedIndex = 2;
            }
            if (MainNavView.SelectedItem == Settings)
            {
                MainTabControl.SelectedIndex = 3;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AccountTabControl.SelectedIndex = 1;
        }
    }
}
