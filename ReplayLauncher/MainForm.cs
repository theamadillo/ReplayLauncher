﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ReplayLauncher
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            GameVersLabel2.Text = gameVersion();
        }

        private string clientexe()
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Riot Games\League of Legends\");
            string lolPath = (string)regKey.GetValue("Path");
            return lolPath + "LeagueClient.exe";
        }

        private string gamePath()
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Riot Games\League of Legends\");
            string lolPath = (string)regKey.GetValue("Path");
            string releasesPath = lolPath + @"RADS\solutions\lol_game_client_sln\releases\";                              //defines path to release folder
            string latestRelease = File.ReadLines(releasesPath + @"releaselisting_EUW").Last();                           //reads latest release number
            string lolDeploy = releasesPath + latestRelease + @"\deploy\";                                                //defines location of the folder of the latest files
            return lolDeploy;
        }

        private string gameVersion()
        {
            try
            {
                string gameVers = FileVersionInfo.GetVersionInfo(gamePath() + "League of Legends.exe").ProductVersion;
                if (gameVers.Substring(0, 4).EndsWith("."))
                {
                    return (gameVers.Substring(0, 3));
                }
                else
                {
                    return (gameVers.Substring(0, 4));
                }
            } 
            catch
            {
                MessageBox.Show("The required game files for League of Legends could not be found! The program will exit now.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return null;
            }
        }

        private string bkpVersion()
        {

            string bkpversion = FileVersionInfo.GetVersionInfo(gamePath() + @"\bkp.League of Legends.exe").ProductVersion;
            if (bkpversion.Substring(0, 4).EndsWith("."))
            {
                return bkpversion.Substring(0, 3);
            }
            else
            {
                return bkpversion.Substring(0, 4);
            }
        }

        private string repName()
        {
            string rp = replayPath.Text;
            return rp;
        }

        private string replayVersion()
        {
            if (repName() != "")
            {
                //does not work in a few cases.             TO-DO: Look for better solution of reading gameLenght and Version of the replay
                 int c = 0;
                int runs = 0;
                byte[] data = new byte[20];

                BinaryReader br = new BinaryReader(new FileStream(repName(), FileMode.Open, FileAccess.Read, FileShare.None));
                br.BaseStream.Position = 0x14A;
                while (c == 0)
                {
                    data[runs] = br.ReadByte();
                    if (data[runs] == 0x22)
                    {
                        c = 1;
                    }
                    runs++;
                }
                br.Close();
                string replvers = Encoding.ASCII.GetString(data.Where(x => x != 0).ToArray());
                if (replvers.Substring(0, 4).EndsWith("."))
                {
                    return replvers.Substring(0, 3);
                }
                else
                {
                    return replvers.Substring(0, 4);
                }

            }
            else { return ""; }
        }

        private bool isRemake()
        {
            if (repName() != "")
            { 
                int c = 0;
                int runs = 0;
                byte[] data = new byte[15];

                BinaryReader br = new BinaryReader(new FileStream(repName(), FileMode.Open, FileAccess.Read, FileShare.None));
                br.BaseStream.Position = 0x12E;
                while (c == 0)
                {
                    data[runs] = br.ReadByte();
                    if (data[runs] == 0x2C)
                    {
                        c = 1;
                    }
                    runs++;
                }
                br.Close();

                char[] i = Encoding.ASCII.GetString(data).ToCharArray();
                string readgamelenght = string.Join("", i).Replace(",", "");
                decimal gamelenght = Decimal.Parse(readgamelenght.Replace(".", ","), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint);
                if (Convert.ToDouble(gamelenght) < 300000)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var file in files)
                {
                    if (System.IO.Path.GetExtension(file).Equals(".rofl", StringComparison.InvariantCultureIgnoreCase))
                    {
                        replayPath.Text = Path.GetFullPath(file);
                        replayPath.SelectionStart = replayPath.Text.Length;
                        replayPath.ScrollToCaret();
                        replayVersion();
                        ReplayVersLabel2.Text = replayVersion();
                    }
                    else
                    {
                        MessageBox.Show("This is not a valid replay file!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders\");
            string mydocpath = (string)regKey.GetValue("Personal");
            ofd.InitialDirectory = mydocpath + @"\League of Legends\Replays";
            ofd.Filter = "League of Legends Replay file | *.rofl";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                replayVersion();
                replayPath.Text = ofd.FileName;
                replayPath.SelectionStart = replayPath.Text.Length;
                replayPath.ScrollToCaret();
                ReplayVersLabel2.Text = replayVersion();
            }
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            GameVersLabel2.Text = gameVersion();
            if (repName() != "" && !isRemake())
            {
                if (string.Equals(gameVersion(), replayVersion()))
                {
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    process.EnableRaisingEvents = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.WorkingDirectory = gamePath();
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/C cd " + "\"" + gamePath() + "\"" + " && " + "\"" + gamePath() + "League of Legends.exe" + "\" " + "\"" + clientexe() + "\" "
                        + "\"" + repName() + "\" " + "\"-UseRads\" ";
                    process.StartInfo = startInfo;
                    process.Start();
                    nowPlaying(true);
                    process.Exited += new EventHandler(process_Exited);
                }
                else
                {
                    DialogResult error1 = MessageBox.Show("The version of the replay does not\nmatch with the version of the game!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (error1 == DialogResult.OK)
                    {
                        DialogResult copyq = MessageBox.Show("Do you want to try to use a compatible League of Legends.exe\nto play this replay anyway?" + 
                            "\nYou might have to patch your game afterwards!", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (copyq == DialogResult.Yes)
                        {
                            if (File.Exists(gamePath() + @"\bkp.League of Legends.exe") && bkpVersion() == replayVersion())
                            {
                                try
                                {
                                    File.Delete(gamePath() + @"\League of Legends.exe");
                                    File.Move(gamePath() + @"\bkp.League of Legends.exe", gamePath() + @"\League of Legends.exe");
                                    playButton_Click(sender, e);
                                }
                                catch
                                {
                                    this.Cursor = Cursors.WaitCursor;
                                    System.Threading.Thread.Sleep(5000);
                                    File.Delete(gamePath() + @"\League of Legends.exe");
                                    File.Move(gamePath() + @"\bkp.League of Legends.exe", gamePath() + @"\League of Legends.exe");
                                    this.Cursor = Cursors.Default;
                                    playButton_Click(sender, e);
                                }
                            }
                            else
                            {
                                replayVersion();
                                string vers = replayVersion();

                                if (File.Exists(@"Resources\LeagueofLegendsexe\LeagueofLegendsPatch" + vers + ".exe"))  // ADD compare versions!!
                                {
                                    if (File.Exists(gamePath() + @"\bkp.League of Legends.exe"))
                                    {
                                        if (bkpVersion().CompareTo(gameVersion()) < 0)
                                        {
                                            File.Delete(gamePath() + @"\bkp.League of Legends.exe");
                                            File.Move(gamePath() + @"\League of Legends.exe", gamePath() + @"\bkp.League of Legends.exe");
                                        }
                                        else
                                        {
                                            try
                                            {
                                                File.Delete(gamePath() + @"\League of Legends.exe");
                                                File.Copy(@"Resources\LeagueofLegendsexe\LeagueofLegendsPatch" + vers + ".exe", gamePath() + @"League of Legends.exe");
                                            }
                                            catch
                                            {
                                                this.Cursor = Cursors.WaitCursor;
                                                System.Threading.Thread.Sleep(5000);
                                                File.Delete(gamePath() + @"\League of Legends.exe");
                                                File.Copy(@"Resources\LeagueofLegendsexe\LeagueofLegendsPatch" + vers + ".exe", gamePath() + @"League of Legends.exe");
                                                this.Cursor = Cursors.Default;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            File.Move(gamePath() + @"\League of Legends.exe", gamePath() + @"\bkp.League of Legends.exe");
                                            File.Copy(@"Resources\LeagueofLegendsexe\LeagueofLegendsPatch" + vers + ".exe", gamePath() + @"League of Legends.exe");
                                        }
                                        catch
                                        {
                                            this.Cursor = Cursors.WaitCursor;
                                            System.Threading.Thread.Sleep(5000);
                                            File.Move(gamePath() + @"\League of Legends.exe", gamePath() + @"\bkp.League of Legends.exe");
                                            File.Copy(@"Resources\LeagueofLegendsexe\LeagueofLegendsPatch" + vers + ".exe", gamePath() + @"League of Legends.exe");
                                            this.Cursor = Cursors.Default;
                                        }
                                    }
                                    playButton_Click(sender, e);
                                }
                                else
                                {
                                    MessageBox.Show("This version is not supported!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if(string.IsNullOrEmpty(repName()))
                {
                   MessageBox.Show("No valid replay file selected!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                if (isRemake())
                {
                    MessageBox.Show("This game was a remake. Remakes are not supported yet!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
        }

        private void MainForm_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            InfoForm frm = new InfoForm();
            frm.ShowDialog();
            e.Cancel = true;
        }

        private void process_Exited(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => nowPlaying(false)));
                return;
            }
            nowPlaying(false);
        }

        private void nowPlaying(bool a)
        {
            if (a == true)
            {
                playButton.Enabled = false;
                browseButton.Enabled = false;
                ClientSize = new System.Drawing.Size(284, 211);
                cPlayingT.Visible = true;
                cPlayingC.Visible = true;
                this.AllowDrop = false;
            }
            if (a == false)
            {
                playButton.Enabled = true;
                browseButton.Enabled = true;
                ClientSize = new System.Drawing.Size(284, 191);
                cPlayingT.Visible = false;
                cPlayingC.Visible = false;
                this.AllowDrop = true;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (File.Exists(gamePath() + @"\League of Legends.exe")
                && File.Exists(gamePath() + @"\bkp.League of Legends.exe"))
            {
                if (bkpVersion().CompareTo(gameVersion()) > 0)
                {
                    try
                    {
                        File.Delete(gamePath() + @"\League of Legends.exe");
                        File.Move(gamePath() + @"\bkp.League of Legends.exe", gamePath() + @"\League of Legends.exe");
                    }
                    catch
                    {
                        try
                        {
                            this.Cursor = Cursors.WaitCursor;
                            this.Enabled = false;
                            System.Threading.Thread.Sleep(5000);
                            File.Delete(gamePath() + @"\League of Legends.exe");
                            File.Move(gamePath() + @"\bkp.League of Legends.exe", gamePath() + @"\League of Legends.exe");
                        }
                        catch
                        {
                            MessageBox.Show("There was a problem in the clean-up process!\nPlease do it manually by deleting \"League of Legends.exe\" " +
                                            "and renaming \"bkp.League of Legends.exe\" to \"League of Legends.exe\"", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Process process = new Process();
                            process.StartInfo.FileName = "explorer.exe";
                            process.StartInfo.Arguments = "\"" + gamePath() + "\"";
                            process.StartInfo.ErrorDialog = true;
                            process.Start();
                        }
                    }
                } else
                {
                    File.Delete(gamePath() + @"\bkp.League of Legends.exe");
                }
            }
        }
    }
}