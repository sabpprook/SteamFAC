using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SteamFAC
{
    public class SteamUser
    {
        public string SteamID { get; set; }
        public string AccountName { get; set; }
        public string PersonaName { get; set; }
        public override string ToString() => $"{AccountName} - {SteamID}";
    }

    public partial class fmGUI : Form
    {
        private SteamUser TargetUser { get; set; }
        private RegistryKey RegistrySteam { get; } = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam", true);

        public fmGUI()
        {
            InitializeComponent();
        }

        private void ShowAndQuit(string message, int exitcode = 0)
        {
            MessageBox.Show(message, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            Environment.Exit(exitcode);
        }

        private void fmGUI_Load(object sender, EventArgs e)
        {
            var steam_path = RegistrySteam.GetValue("SteamPath", "") as string;
            if (!Directory.Exists(steam_path))
                ShowAndQuit("Steam not installed", 1);
            var steam_login_file = steam_path + @"\config\loginusers.vdf";
            if (!File.Exists(steam_login_file))
                ShowAndQuit("No Steam user found", 1);
            var text = File.ReadAllText(steam_login_file);
            try
            {
                var steam_id_matches = Regex.Matches(text, "\"(\\d{12,20})\"");
                var account_name_matches = Regex.Matches(text, "\"AccountName\"[\\t ]+\"([^\"^\\t^\\n]+)\"");
                var persona_name_matches = Regex.Matches(text, "\"PersonaName\"[\\t ]+\"([^\"^\\t^\\n]+)\"");
                for (int i = 0; i < steam_id_matches.Count; i++)
                {
                    comboBox1.Items.Add(new SteamUser()
                    {
                        SteamID = steam_id_matches[i].Groups[1].Value,
                        AccountName = account_name_matches[i].Groups[1].Value,
                        PersonaName = persona_name_matches[i].Groups[1].Value
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ShowAndQuit("Parsing loginusers.vdf failed", 1);
            }
            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            TargetUser = comboBox1.SelectedItem as SteamUser;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var steam_exe = RegistrySteam.GetValue("SteamExe", "") as string;
            while (Process.GetProcessesByName("Steam").Length > 0)
            {
                Process.Start(steam_exe, "-shutdown");
                await Task.Delay(1000);
            }
            var auto_login_user = RegistrySteam.GetValue("AutoLoginUser", "") as string;
            if (string.IsNullOrEmpty(auto_login_user))
                ShowAndQuit("Registry 'AutoLoginUser' not found or set", 1);
            RegistrySteam.SetValue("AutoLoginUser", TargetUser.AccountName);
            Process.Start(steam_exe);
            Environment.Exit(0);
        }
    }
}
