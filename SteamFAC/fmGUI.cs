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
    public partial class fmGUI : Form
    {
        private SteamUser TargetUser { get; set; }

        public fmGUI()
        {
            InitializeComponent();
        }

        private void fmGUI_Load(object sender, EventArgs e)
        {
            var steam_login_file = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam\config\loginusers.vdf";
            if (!File.Exists(steam_login_file))
            {
                MessageBox.Show("No user found", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                Environment.Exit(1);
            }
            var text = File.ReadAllText(steam_login_file);
            try
            {
                var steam_id_matches = Regex.Matches(text, "\"(\\d{12,20})\"");
                var account_name_matches = Regex.Matches(text, "\"AccountName\"[\\t ]+\"(\\w+)\"");
                var persona_name_matches = Regex.Matches(text, "\"PersonaName\"[\\t ]+\"([^\"^\\t^\\n]+)\"");
                for (int i = 0; i < steam_id_matches.Count; i++)
                {
                    var user = new SteamUser()
                    {
                        SteamID = steam_id_matches[i].Groups[1].Value,
                        AccountName = account_name_matches[i].Groups[1].Value,
                        PersonaName = persona_name_matches[i].Groups[1].Value
                    };
                    comboBox1.Items.Add(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Parsing loginusers.vdf failed", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                Environment.Exit(1);
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
            var steam_exe = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam\steam.exe";
            Process[] process_list;
            while ((process_list = Process.GetProcessesByName("Steam")).Length > 0)
            {
                foreach (Process p in process_list)
                {
                    while(!p.HasExited)
                    {
                        p.Kill();
                        await Task.Delay(500);
                    }
                }
                await Task.Delay(100);
            }
            var steam_reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam", true);
            var auto_login_user = steam_reg.GetValue("AutoLoginUser", "") as string;
            if (string.IsNullOrEmpty(auto_login_user))
            {
                MessageBox.Show("Registry 'AutoLoginUser' not found or set", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                Environment.Exit(1);
            }
            steam_reg.SetValue("AutoLoginUser", TargetUser.AccountName);
            Process.Start(steam_exe);
            Environment.Exit(0);
        }
    }

    public class SteamUser
    {
        public string SteamID { get; set; }
        public string AccountName { get; set; }
        public string PersonaName { get; set; }
        public override string ToString()
        {
            return $"{AccountName} - {SteamID}";
        }
    }
}
