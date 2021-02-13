using MySql.Data.MySqlClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

namespace Time_tracking
{
    class Program
    {
        public static string[] Windows_Array = {
   "Microsoft Visual Studio",
   "Visual Studio Code",
   "Google Chrome"
  };
        public static string[] Ignore_Array = {
   "Microsoft Visual Studio",
   "Visual Studio Code",
   "Welcome - Visual Studio Code",
   "- Google Chrome",
   "chrome: - Google Chrome"
  };

        public static string DBserver = "localhost";
        public static string DBname = "test";
        public static string DBuser = "root";
        public static string DBpass = "";


        public static string DB = $"server={DBserver};userid={DBuser};password={DBpass};database={DBname}";
        public static Stopwatch stopwatch = new Stopwatch();
        public static string Active_Window_Name = "";

        static void Main(string[] args)
        {
            Register();
            Activity_Monitor();
        }

        public static void Register()
        {
            ProcessStartInfo reg = new ProcessStartInfo("snoretoast.exe", "-install \"Coding Time Tracker.ink\" \"" + Application.ExecutablePath + "\" \"Coding Time Tracker\"");
            reg.UseShellExecute = false;
            reg.CreateNoWindow = true;
            reg.WindowStyle = ProcessWindowStyle.Hidden;
            Process preg = new Process();
            preg.StartInfo = reg;
            preg.Start();
        }

        public static void Save_Output(MySqlConnection conn, string name, string time, string date)
        {
            string cmdStr = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{DBname}' AND table_name = '{name}'";
            MySqlCommand cmd = new MySqlCommand(cmdStr, conn);
            MySqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            int count = reader.GetInt32(0);

            if (count == 0)
            {
                string callback = "";
                if (!name.Contains("Google Chrome"))
                {
                    string ignored = File.ReadAllText("ignore.ini");
                    if (!ignored.Contains(name))
                    {
                        ProcessStartInfo processStartInfo = new ProcessStartInfo("snoretoast.exe", "-t \"Track Your New Project ?\" -m \"" + name + "\" -p icon.png -b Yep;Nope -pipeName callback.ini -appID \"Coding Time Tracker\"");
                        processStartInfo.UseShellExecute = false;
                        processStartInfo.CreateNoWindow = true;
                        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        processStartInfo.RedirectStandardOutput = true;
                        Process p = new Process();
                        p.StartInfo = processStartInfo;
                        p.Start();
                        p.WaitForExit();
                        Thread.Sleep(1000);
                        callback = File.ReadAllText("callback.ini");
                    }
                    }
                    else
                    {
                    callback = "Y";
                    }
                    if (callback.Contains("Y"))
                    {
                        // No such data table exists!
                        reader.Close();
                        var cmnd = new MySqlCommand();
                        cmnd.Connection = conn;
                        cmnd.CommandText = @"CREATE TABLE `" + name + "` (id INTEGER PRIMARY KEY AUTO_INCREMENT, time TEXT, date TEXT)";
                        cmnd.ExecuteNonQuery();
                        cmnd.CommandText = "INSERT INTO `" + name + "` (time, date) VALUES('" + time + "', '" + date + "')";
                        cmnd.ExecuteNonQuery();
                        Console.WriteLine(name + " | " + time + " | " + date);
                        callback = "";
                    }
                    else
                    {
                    string ignored = File.ReadAllText("ignore.ini");
                    if (!ignored.Contains(name))
                    {
                        using (StreamWriter w = File.AppendText("ignore.ini"))
                        {
                            w.Write("[" + name + "]");
                            w.WriteLine();
                        }
                    }
                }
            }
            else if (count == 1)
            {
                // Such data table exists!
                reader.Close();
                var cmnd = new MySqlCommand();
                cmnd.Connection = conn;
                cmnd.CommandText = "INSERT INTO `" + name + "` (time, date) VALUES('" + time + "', '" + date + "')";
                cmnd.ExecuteNonQuery();
                Console.WriteLine(name + " | " + time + " | " + date);
            }
        }

        public static void Activity_Monitor()
        {
            while (true)
            {
                string New_Window_Name = GetActiveWindowTitle();


                //filters start


                try
                {
                    if (New_Window_Name.Contains("Google Chrome"))
                    {
                        New_Window_Name = Chrome_domain() + " - Google Chrome";
                    }
                }
                catch (Exception) { }

                try
                {
                    if (New_Window_Name.Contains("Microsoft Visual Studio"))
                    {
                        int num = countLetters(New_Window_Name, " - ");
                        string first = New_Window_Name.Split('-')[num];
                        string last = New_Window_Name.Split('-')[num - 1];

                        if (first.EndsWith(" "))
                        {
                            first = Regex.Replace(first, @"\s$", "");
                        }

                        if (last.StartsWith(" "))
                        {
                            last = last.TrimStart();
                        }

                        New_Window_Name = first + " - " + last;
                    }
                }
                catch (Exception) { }

                try
                {
                    if (New_Window_Name.Contains("Visual Studio Code"))
                    {
                        New_Window_Name = New_Window_Name.Replace("?", "");
                        int num = countLetters(New_Window_Name, " - ");
                        string first = New_Window_Name.Split('-')[num];
                        string last = New_Window_Name.Split('-')[num - 1];

                        if (first.EndsWith(" "))
                        {
                            first = Regex.Replace(first, @"\s$", "");
                        }

                        if (last.StartsWith(" "))
                        {
                            last = last.TrimStart();
                        }

                        New_Window_Name = first + " - " + last;
                    }
                }
                catch (Exception) { }

                try
                {
                    New_Window_Name = New_Window_Name.Replace(" (Running)", "");
                }
                catch (Exception) { }

                try
                {
                    if (New_Window_Name.StartsWith(" "))
                    {
                        New_Window_Name = New_Window_Name.TrimStart();
                    }
                }
                catch (Exception) { }

                try
                {
                    if (New_Window_Name.EndsWith(" "))
                    {
                        New_Window_Name = Regex.Replace(New_Window_Name, @"\s$", "");
                    }
                }
                catch (Exception) { }


                //filters end

                stopwatch.Start();
                string date = DateTime.UtcNow.ToString("MM-dd-yyyy");
                if (Active_Window_Name != New_Window_Name)
                {
                    string Last_Window = Active_Window_Name;
                    Active_Window_Name = New_Window_Name;
                    stopwatch.Stop();
                    TimeSpan ts = stopwatch.Elapsed;

                    if (!string.IsNullOrEmpty(Last_Window))
                    {
                        foreach (string x in Windows_Array)
                        {
                            if (Last_Window.ToLower().Contains(x.ToLower()))
                            {
                                int attemp = 0;
                                foreach (string y in Ignore_Array)
                                {
                                    if (Last_Window.ToLower() != y.ToLower())
                                    {
                                        attemp += 1;
                                        if (attemp == Ignore_Array.Length)
                                        {
                                            if (!Last_Window.Contains("Untitled"))
                                            {
                                            try
                                            {
                                                var con = new MySqlConnection(DB);
                                                con.Open();
                                                Save_Output(con, Last_Window, string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds), date);
                                                con.Close();
                                            }
                                            catch (Exception) { }
                                        }
                                    }
                                    }
                                }
                                attemp = 0;
                                stopwatch.Reset();
                            }
                        }
                    }

                }



                
                Thread.Sleep(1000);
            }
        }

        public static int countLetters(string word, string countableLetters)
        {
            int count = 0;
            foreach (char c in word)
            {
                if (countableLetters.Contains(c))
                    count++;
            }
            return count;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return "";
        }

        public static string Chrome_domain()
        {
            Process[] procsChrome = Process.GetProcessesByName("chrome");
            foreach (Process chrome in procsChrome)
            {
                if (chrome.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                AutomationElement elm = AutomationElement.FromHandle(chrome.MainWindowHandle);
                AutomationElement elmUrlBar = elm.FindFirst(TreeScope.Descendants,
                  new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
                if (elmUrlBar != null)
                {
                    AutomationPattern[] patterns = elmUrlBar.GetSupportedPatterns();
                    if (patterns.Length > 0)
                    {
                        ValuePattern val = (ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0]);
                        string domain = val.Current.Value;
                        domain = domain.Split('/')[0];
                        return domain;
                    }
                }
            }
            return "";
        }
    }
}