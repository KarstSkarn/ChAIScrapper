using Microsoft.VisualBasic.ApplicationServices;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ChAIScrapperWF
{
    public partial class ChAIWF : Form
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private System.Windows.Forms.Timer timer;
        private TimeSpan elapsedTime;
        public ChAIWF()
        {
            InitializeComponent();

            elapsedTime = TimeSpan.Zero;
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1000 ms = 1 segundo
            timer.Tick += Timer_Tick;
            timer.Start();

            ChAIDiscordBot.mainForm = this;

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "LastConfigPath.txt")))
            {
                try
                {
                    string fileRoute = Path.Combine(Directory.GetCurrentDirectory(), "LastConfigPath.txt");
                    string fileContent = File.Exists(fileRoute)
                    ? File.ReadAllText(fileRoute)
                    : string.Empty;
                    if (fileContent != string.Empty && File.Exists(fileContent))
                    {
                        ChAIDataStructures.ConfigData newData = ChAIAppend.LoadObjectFromFile<ChAIDataStructures.ConfigData>(fileContent);

                        textBoxDiscordToken.Text = newData.DISCORDTOKEN;
                        numericIdleMin.Value = newData.IDLEMIN;
                        numericIdleMax.Value = newData.IDLEMAX;
                        textBoxURL.Text = newData.CHAIURL;
                        textBoxDiscordTextChannelID.Text = newData.DISCORDCHANNELID.ToString();
                        textBoxChromiumPort.Text = newData.CHROMIUMPORT;
                        textBoxDiscordBotNameStencil.Text = newData.USERNAMESTENCIL;
                        boolVoiceNotes.Checked = newData.ALLOWAUDIOS;
                        boolDMs.Checked = newData.ALLOWDMS;
                        boolAdminOnlyLock.Checked = newData.ADMINISTRATIVELOCK;
                        boolYTVideos.Checked = newData.ALLOWYTVIDEOS;
                        textBoxBotBriefing.Text = newData.INITIALBOTBRIEFING;
                        textBoxDiscordServerID.Text = newData.DISCORDSERVERID.ToString();
                        textBoxDiscriminatoryString.Text = newData.DISCRIMINATORYSTRING;
                        textBoxTimeout.Text = newData.TIMEOUT.ToString();
                        boolDiscordReactions.Checked = newData.DISCORDREACTIONS;
                        textBoxYTLapse.Text = newData.VIDEOSINTERVAL.ToString();
                        textBoxLangCode.Text = newData.LANGCODE;
                        if (!newData.DISCRIMINATORYEXCLUSIVE)
                        {
                            buttonDiscriminatoryString.Text = "Ignore";
                            Global.discriminatoryExclusive = false;
                        }
                        else
                        {
                            buttonDiscriminatoryString.Text = "Listen Only";
                            Global.discriminatoryExclusive = true;
                        }

                        listBoxAdmin.Items.Clear();
                        listBoxIgnore.Items.Clear();

                        foreach (string entry in newData.ADMINISTRATIVEUSERSLIST)
                        {
                            listBoxAdmin.Items.Add(entry.ToString());
                        }

                        foreach (string entry in newData.USERSIGNORELIST)
                        {
                            listBoxIgnore.Items.Add(entry.ToString());
                        }
                    }
                    else
                    {
                        File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "LastConfigPath.txt"));
                    }
                }
                catch
                {
                    File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "LastConfigPath.txt"));
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Global.launchedFlag)
            {
                // Sumamos 1 segundo al tiempo total
                elapsedTime = elapsedTime.Add(TimeSpan.FromSeconds(1));

                // Mostramos el tiempo en una Label (ejemplo)
                labelTime.Text = string.Format("{0:D2}D:{1:D2}H:{2:D2}M:{3:D2}S",
                    elapsedTime.Days,
                    elapsedTime.Hours,
                    elapsedTime.Minutes,
                    elapsedTime.Seconds);
            }
            else
            {
                labelTime.Text = "-";
            }
        }

        private void buttonSaveConfig_Click(object sender, EventArgs e)
        {
            ChAIDataStructures.ConfigData newData = new ChAIDataStructures.ConfigData();
            try
            {
                newData.DISCORDTOKEN = textBoxDiscordToken.Text;
                newData.IDLEMIN = (int)numericIdleMin.Value;
                newData.IDLEMAX = (int)numericIdleMax.Value;
                newData.CHAIURL = textBoxURL.Text;
                newData.DISCORDCHANNELID = ulong.Parse(textBoxDiscordTextChannelID.Text);
                newData.CHROMIUMPORT = textBoxChromiumPort.Text;
                newData.USERNAMESTENCIL = textBoxDiscordBotNameStencil.Text;
                newData.ALLOWAUDIOS = boolVoiceNotes.Checked;
                newData.ALLOWDMS = boolDMs.Checked;
                newData.ADMINISTRATIVELOCK = boolAdminOnlyLock.Checked;
                newData.ALLOWYTVIDEOS = boolYTVideos.Checked;
                newData.INITIALBOTBRIEFING = textBoxBotBriefing.Text;
                newData.DISCORDSERVERID = ulong.Parse(textBoxDiscordServerID.Text);
                newData.DISCRIMINATORYSTRING = textBoxDiscriminatoryString.Text;
                newData.TIMEOUT = int.Parse(textBoxTimeout.Text);
                newData.DISCORDREACTIONS = boolDiscordReactions.Checked;
                newData.VIDEOSINTERVAL = int.Parse(textBoxYTLapse.Text);
                newData.LANGCODE = textBoxLangCode.Text;
                if (buttonDiscriminatoryString.Text.Contains("Ignore"))
                {
                    newData.DISCRIMINATORYEXCLUSIVE = false;
                }
                else
                {
                    newData.DISCRIMINATORYEXCLUSIVE = true;
                }

                List<string> adminList = new List<string>();
                List<string> ignoreList = new List<string>();

                foreach (string entry in listBoxAdmin.Items)
                {
                    adminList.Add(entry.ToString());
                }

                foreach (string entry in listBoxIgnore.Items)
                {
                    ignoreList.Add(entry.ToString());
                }

                newData.ADMINISTRATIVEUSERSLIST = adminList;
                newData.USERSIGNORELIST = ignoreList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("One of more parameters are not correctly formated or undefined!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML Files (*.xml)|*.xml";
            saveFileDialog.Title = "Save Configuration File";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileRoute = saveFileDialog.FileName;

                ChAIAppend.SaveObjectToFile<ChAIDataStructures.ConfigData>(newData, fileRoute);

                MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "LastConfigPath.txt"), fileRoute);
            }
        }

        private void buttonLoadConfig_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml";
            openFileDialog.Title = "Open File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileRoute = openFileDialog.FileName;
                ChAIDataStructures.ConfigData newData = ChAIAppend.LoadObjectFromFile<ChAIDataStructures.ConfigData>(fileRoute);

                textBoxDiscordToken.Text = newData.DISCORDTOKEN;
                numericIdleMin.Value = newData.IDLEMIN;
                numericIdleMax.Value = newData.IDLEMAX;
                textBoxURL.Text = newData.CHAIURL;
                textBoxDiscordTextChannelID.Text = newData.DISCORDCHANNELID.ToString();
                textBoxChromiumPort.Text = newData.CHROMIUMPORT;
                textBoxDiscordBotNameStencil.Text = newData.USERNAMESTENCIL;
                boolVoiceNotes.Checked = newData.ALLOWAUDIOS;
                boolDMs.Checked = newData.ALLOWDMS;
                boolAdminOnlyLock.Checked = newData.ADMINISTRATIVELOCK;
                boolYTVideos.Checked = newData.ALLOWYTVIDEOS;
                textBoxBotBriefing.Text = newData.INITIALBOTBRIEFING;
                textBoxDiscordServerID.Text = newData.DISCORDSERVERID.ToString();
                textBoxDiscriminatoryString.Text = newData.DISCRIMINATORYSTRING;
                textBoxTimeout.Text = newData.TIMEOUT.ToString();
                boolDiscordReactions.Checked = newData.DISCORDREACTIONS;
                textBoxYTLapse.Text = newData.VIDEOSINTERVAL.ToString();
                textBoxLangCode.Text = newData.LANGCODE;
                if (!newData.DISCRIMINATORYEXCLUSIVE)
                {
                    buttonDiscriminatoryString.Text = "Ignore";
                    Global.discriminatoryExclusive = false;
                }
                else
                {
                    buttonDiscriminatoryString.Text = "Listen Only";
                    Global.discriminatoryExclusive = true;
                }

                listBoxAdmin.Items.Clear();
                listBoxIgnore.Items.Clear();

                foreach (string entry in newData.ADMINISTRATIVEUSERSLIST)
                {
                    listBoxAdmin.Items.Add(entry.ToString());
                }

                foreach (string entry in newData.USERSIGNORELIST)
                {
                    listBoxIgnore.Items.Add(entry.ToString());
                }

                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "LastConfigPath.txt"), fileRoute);
            }
        }

        private void buttonAddAdmin_Click(object sender, EventArgs e)
        {
            string composite = "";
            composite = textBoxAdminDiscordID.Text;
            try
            {
                ulong.Parse(composite);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The given Discord ID seems not valid or has the wrong format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (textBoxAdminCosmeticName.Text != "")
            {
                composite = composite + " (" + textBoxAdminCosmeticName.Text + ")";
                textBoxAdminCosmeticName.Text = "";
            }
            listBoxAdmin.Items.Add(composite);
            textBoxAdminDiscordID.Text = "";
        }

        private void buttonDeleteAdmin_Click(object sender, EventArgs e)
        {
            if (listBoxAdmin.SelectedItem != null)
            {
                listBoxAdmin.Items.Remove(listBoxAdmin.SelectedItem);
            }
        }

        private void buttonAddIgnore_Click(object sender, EventArgs e)
        {
            string composite = "";
            composite = textBoxIgnoreID.Text;
            try
            {
                ulong.Parse(composite);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The given Discord ID seems not valid or has the wrong format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (textBoxIgnoreCosmetic.Text != "")
            {
                composite = composite + " (" + textBoxIgnoreCosmetic.Text + ")";
                textBoxIgnoreCosmetic.Text = "";
            }
            listBoxIgnore.Items.Add(composite);
            textBoxIgnoreID.Text = "";
        }

        private void buttonDeleteIgnore_Click(object sender, EventArgs e)
        {
            if (listBoxIgnore.SelectedItem != null)
            {
                listBoxIgnore.Items.Remove(listBoxIgnore.SelectedItem);
            }
        }
        private void buttonLaunchChromium_Click(object sender, EventArgs e)
        {
            ChAIO.textBoxGeneral = this.textBoxGeneralOutput;
            ChAIO.textBoxDiscord = this.textBoxDiscordOutput;
            ChAIO.textBoxScrapper = this.textBoxScrapperOutput;
            ChAIDiscordBot.botPanelImage = this.pictureBot;
            ChAIDataStructures.ConfigData newData = new ChAIDataStructures.ConfigData();
            try
            {
                newData.DISCORDTOKEN = textBoxDiscordToken.Text;
                newData.IDLEMIN = (int)numericIdleMin.Value;
                newData.IDLEMAX = (int)numericIdleMax.Value;
                newData.CHAIURL = textBoxURL.Text;
                newData.DISCORDCHANNELID = ulong.Parse(textBoxDiscordTextChannelID.Text);
                newData.CHROMIUMPORT = textBoxChromiumPort.Text;
                newData.USERNAMESTENCIL = textBoxDiscordBotNameStencil.Text;
                newData.ALLOWAUDIOS = boolVoiceNotes.Checked;
                newData.ALLOWDMS = boolDMs.Checked;
                newData.ADMINISTRATIVELOCK = boolAdminOnlyLock.Checked;
                newData.ALLOWYTVIDEOS = boolYTVideos.Checked;
                newData.INITIALBOTBRIEFING = textBoxBotBriefing.Text;
                newData.DISCORDSERVERID = ulong.Parse(textBoxDiscordServerID.Text);
                newData.DISCRIMINATORYSTRING = textBoxDiscriminatoryString.Text;
                newData.TIMEOUT = int.Parse(textBoxTimeout.Text);
                newData.DISCORDREACTIONS = boolDiscordReactions.Checked;
                newData.VIDEOSINTERVAL = int.Parse(textBoxYTLapse.Text);
                newData.LANGCODE = textBoxLangCode.Text;
                if (buttonDiscriminatoryString.Text.Contains("Ignore"))
                {
                    newData.DISCRIMINATORYEXCLUSIVE = false;
                }
                else
                {
                    newData.DISCRIMINATORYEXCLUSIVE = true;
                }

                List<string> adminList = new List<string>();
                List<string> ignoreList = new List<string>();

                newData.ADMINISTRATIVEUSERSLIST = adminList;
                newData.USERSIGNORELIST = ignoreList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("One of more parameters are not correctly formated or undefined in the configuration tab!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string batContent =
            @"@echo off
            start """" "".\Chromium\chrome.exe"" ^
                --remote-debugging-port=" + newData.CHROMIUMPORT + @" ^
                --user-data-dir="".\Chromium\UserData"" ^
                --disable-background-timer-throttling ^
                --disable-backgrounding-occluded-windows ^
                --disable-renderer-backgrounding ^
                --disable-component-extensions-with-background-pages
            exit";
            string path = Path.Combine(Directory.GetCurrentDirectory(), "ChromiumLauncher.bat");
            File.WriteAllText(path, batContent);
            //MessageBox.Show(path + "\n" + batContent, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }

        public void ForceSaveCurrentData()
        {
            ChAIDataStructures.ConfigData newData = new ChAIDataStructures.ConfigData();
            try
            {
                newData.DISCORDTOKEN = textBoxDiscordToken.Text;
                newData.IDLEMIN = (int)numericIdleMin.Value;
                newData.IDLEMAX = (int)numericIdleMax.Value;
                newData.CHAIURL = textBoxURL.Text;
                newData.DISCORDCHANNELID = ulong.Parse(textBoxDiscordTextChannelID.Text);
                newData.CHROMIUMPORT = textBoxChromiumPort.Text;
                newData.USERNAMESTENCIL = textBoxDiscordBotNameStencil.Text;
                newData.ALLOWAUDIOS = boolVoiceNotes.Checked;
                newData.ALLOWDMS = boolDMs.Checked;
                newData.ADMINISTRATIVELOCK = boolAdminOnlyLock.Checked;
                newData.ALLOWYTVIDEOS = boolYTVideos.Checked;
                newData.INITIALBOTBRIEFING = textBoxBotBriefing.Text;
                newData.DISCORDSERVERID = ulong.Parse(textBoxDiscordServerID.Text);
                newData.DISCRIMINATORYSTRING = textBoxDiscriminatoryString.Text;
                newData.TIMEOUT = int.Parse(textBoxTimeout.Text);
                newData.DISCORDREACTIONS = boolDiscordReactions.Checked;
                newData.VIDEOSINTERVAL = int.Parse(textBoxYTLapse.Text);
                newData.LANGCODE = textBoxLangCode.Text;
                if (buttonDiscriminatoryString.Text.Contains("Ignore"))
                {
                    newData.DISCRIMINATORYEXCLUSIVE = false;
                }
                else
                {
                    newData.DISCRIMINATORYEXCLUSIVE = true;
                }

                List<string> adminList = new List<string>();
                List<string> ignoreList = new List<string>();

                foreach (string entry in listBoxAdmin.Items)
                {
                    adminList.Add(entry.ToString());
                }

                foreach (string entry in listBoxIgnore.Items)
                {
                    ignoreList.Add(entry.ToString());
                }

                newData.ADMINISTRATIVEUSERSLIST = adminList;
                newData.USERSIGNORELIST = ignoreList;
            }
            catch (Exception ex)
            {
                return;
            }

            string fileRoute = Path.Combine(Directory.GetCurrentDirectory(), "LastConfigPath.txt");
            string fileContent = File.Exists(fileRoute)
            ? File.ReadAllText(fileRoute)
            : string.Empty;
            if (fileContent != string.Empty)
            {
                ChAIAppend.SaveObjectToFile<ChAIDataStructures.ConfigData>(newData, fileContent);
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "LastConfigPath.txt"), fileContent);
            }
        }

        private void buttonRunChAIScrapper_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
            }
            ChAIO.textBoxGeneral = this.textBoxGeneralOutput;
            ChAIO.textBoxDiscord = this.textBoxDiscordOutput;
            ChAIO.textBoxScrapper = this.textBoxScrapperOutput;
            ChAIDiscordBot.botPanelImage = this.pictureBot;
            if (!Global.launchedFlag)
            {
                ChAIDataStructures.ConfigData newData = new ChAIDataStructures.ConfigData();
                try
                {
                    newData.DISCORDTOKEN = textBoxDiscordToken.Text;
                    newData.IDLEMIN = (int)numericIdleMin.Value;
                    newData.IDLEMAX = (int)numericIdleMax.Value;
                    newData.CHAIURL = textBoxURL.Text;
                    newData.DISCORDCHANNELID = ulong.Parse(textBoxDiscordTextChannelID.Text);
                    newData.CHROMIUMPORT = textBoxChromiumPort.Text;
                    newData.USERNAMESTENCIL = textBoxDiscordBotNameStencil.Text;
                    newData.ALLOWAUDIOS = boolVoiceNotes.Checked;
                    newData.ALLOWDMS = boolDMs.Checked;
                    newData.ADMINISTRATIVELOCK = boolAdminOnlyLock.Checked;
                    newData.ALLOWYTVIDEOS = boolYTVideos.Checked;
                    newData.INITIALBOTBRIEFING = textBoxBotBriefing.Text;
                    newData.DISCORDSERVERID = ulong.Parse(textBoxDiscordServerID.Text);
                    newData.DISCRIMINATORYSTRING = textBoxDiscriminatoryString.Text;
                    newData.TIMEOUT = int.Parse(textBoxTimeout.Text);
                    newData.DISCORDREACTIONS = boolDiscordReactions.Checked;
                    newData.VIDEOSINTERVAL = int.Parse(textBoxYTLapse.Text);
                    newData.LANGCODE = textBoxLangCode.Text;
                    if (buttonDiscriminatoryString.Text.Contains("Ignore"))
                    {
                        newData.DISCRIMINATORYEXCLUSIVE = false;
                    }
                    else
                    {
                        newData.DISCRIMINATORYEXCLUSIVE = true;
                    }

                    List<string> adminList = new List<string>();
                    List<string> ignoreList = new List<string>();

                    foreach (string entry in listBoxAdmin.Items)
                    {
                        adminList.Add(entry.ToString());
                    }

                    foreach (string entry in listBoxIgnore.Items)
                    {
                        ignoreList.Add(entry.ToString());
                    }

                    newData.ADMINISTRATIVEUSERSLIST = adminList;
                    newData.USERSIGNORELIST = ignoreList;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("One of more parameters are not correctly formated or undefined!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                textBoxDiscordToken.Enabled = false;
                textBoxDiscordTextChannelID.Enabled = false;
                textBoxChromiumPort.Enabled = false;
                textBoxDiscordBotNameStencil.Enabled = false;
                textBoxBotBriefing.Enabled = false;
                buttonLaunchChromium.Enabled = false;
                buttonSaveConfig.Enabled = false;
                buttonLoadConfig.Enabled = false;
                boolChangeProfilePicture.Enabled = false;
                boolChangeUserName.Enabled = false;
                textBoxURL.Enabled = false;
                textBoxDiscordServerID.Enabled = false;
                textBoxTimeout.Enabled = false;
                numericIdleMax.Enabled = false;
                numericIdleMin.Enabled = false;
                textBoxDiscriminatoryString.Enabled = false;
                buttonDiscriminatoryString.Enabled = false;
                groupBoxAdminList.Enabled = false;
                groupBoxIgnoreList.Enabled = false;
                boolDiscordReactions.Enabled = false;
                boolDiscordReactions.Enabled = false;
                textBoxYTLapse.Enabled = false;
                textBoxLangCode.Enabled = false;

                buttonRunChAIScrapper.Text = "Stop ChAIScrapper";

                Global.launchedFlag = true;
                Global.timeout = newData.TIMEOUT;
                Global.characterAIChatURL = newData.CHAIURL;
                Global.allowBotAudios = newData.ALLOWAUDIOS;
                Global.allowYTVirtualWatch = newData.ALLOWYTVIDEOS;
                Global.discordBotToken = newData.DISCORDTOKEN;
                Global.discordChannelID = newData.DISCORDCHANNELID;
                Global.idleMin = newData.IDLEMIN;
                Global.idleMax = newData.IDLEMAX;
                Global.chromiumPort = newData.CHROMIUMPORT;
                Global.chromeDebuggerAddress = "localhost:" + Global.chromiumPort;
                Global.initialBotBriefing = newData.INITIALBOTBRIEFING;
                Global.discordServerID = newData.DISCORDSERVERID;
                Global.discriminatoryString = newData.DISCRIMINATORYSTRING;
                Global.discriminatoryExclusive = newData.DISCRIMINATORYEXCLUSIVE;
                Global.discordReactions = newData.DISCORDREACTIONS;
                Global.botYTVirtualWatchPace = TimeSpan.FromSeconds(Convert.ToDouble(newData.VIDEOSINTERVAL));
                Global.preferredLanguageCode = newData.LANGCODE;

                ChAIWebScrapper.AINameLabel = this.labelAIName;
                ChAIDiscordBot.AIStatusLabel = this.labelAIStatus;
                ChAIDiscordBot.DMCounterLabel = this.labelDMQueue;
                ChAIDiscordBot.characterURLTextBox = this.textBoxURL;
                ChAIDiscordBot.ResponsesCounterLabel = this.labelResponsesCounter;

                ChAIWebScrapper.AIStatusLabel = this.labelAIStatus;
                ChAIWebScrapper.DMCounterLabel = this.labelDMQueue;
                ChAIWebScrapper.ResponsesCounterLabel = this.labelResponsesCounter;

                ChAIDiscordBot.ignoreListBox = this.listBoxIgnore;

                List<ulong> internalAdminList = new List<ulong>();

                foreach (var item in listBoxAdmin.Items)
                {
                    try
                    {
                        string cleaned = Regex.Replace(item.ToString(), @"\s*\(.*?\)", "");
                        internalAdminList.Add(ulong.Parse(cleaned));
                    }
                    catch { }
                }

                Global.internalAdminList = internalAdminList;

                List<ulong> internalIgnoreList = new List<ulong>();

                foreach (var item in listBoxIgnore.Items)
                {
                    try
                    {
                        string cleaned = Regex.Replace(item.ToString(), @"\s*\(.*?\)", "");
                        internalIgnoreList.Add(ulong.Parse(cleaned));
                    }
                    catch { }
                }

                Global.internalIgnoreList = internalIgnoreList;

                var result = MessageBox.Show("Do you want the bot to receive the initial briefing?", "Initial Bot Briefing", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Global.briefTheBot = true;
                }
                else
                {
                    Global.briefTheBot = false;
                }

                Global.launchedFlag = true;
                labelStatus.Text = "• Running!";
                labelStatus.ForeColor = System.Drawing.Color.FromArgb(0, 250, 154);
                groupBoxStatusDisplay.Enabled = true;

                _ = Task.Run(() => ChAILauncher.MonitorTasks(cancellationTokenSource));
            }
            else
            {
                cancellationTokenSource.Cancel();
                textBoxDiscordToken.Enabled = true;
                textBoxDiscordTextChannelID.Enabled = true;
                textBoxChromiumPort.Enabled = true;
                textBoxDiscordBotNameStencil.Enabled = true;
                textBoxBotBriefing.Enabled = true;
                buttonLaunchChromium.Enabled = true;
                buttonSaveConfig.Enabled = true;
                buttonLoadConfig.Enabled = true;
                boolChangeProfilePicture.Enabled = true;
                boolChangeUserName.Enabled = true;
                textBoxURL.Enabled = true;
                textBoxDiscordServerID.Enabled = true;
                groupBoxStatusDisplay.Enabled = false;
                textBoxTimeout.Enabled = true;
                numericIdleMax.Enabled = false;
                numericIdleMin.Enabled = false;
                textBoxDiscriminatoryString.Enabled = true;
                buttonDiscriminatoryString.Enabled = true;
                groupBoxAdminList.Enabled = true;
                groupBoxIgnoreList.Enabled = true;
                boolDiscordReactions.Enabled = true;
                textBoxYTLapse.Enabled = true;
                textBoxLangCode.Enabled = true;

                buttonRunChAIScrapper.Text = "Run ChAIScrapper";

                Global.launchedFlag = false;
                labelStatus.Text = "• Offline";
                labelStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            }
        }

        private void boolHideToken_CheckedChanged(object sender, EventArgs e)
        {
            if (boolHideToken.Checked)
            {
                textBoxDiscordToken.UseSystemPasswordChar = true;
                textBoxDiscordToken.PasswordChar = '\0';
            }
            else
            {
                textBoxDiscordToken.UseSystemPasswordChar = false;
                textBoxDiscordToken.PasswordChar = '\0';
            }
        }

        private void buttonDiscriminatoryString_Click(object sender, EventArgs e)
        {
            if (!Global.discriminatoryExclusive)
            {
                buttonDiscriminatoryString.Text = "Listen Only";
                Global.discriminatoryExclusive = true;
            }
            else
            {
                buttonDiscriminatoryString.Text = "Ignore";
                Global.discriminatoryExclusive = false;
            }
        }

        private void boolAdminOnlyLock_CheckedChanged(object sender, EventArgs e)
        {
            lock (Global.lockInternalData)
            {
                if (boolAdminOnlyLock.Checked) { Global.adminLockFlag = true; } else { Global.adminLockFlag = false; }
            }
        }

        private void boolYTVideos_CheckedChanged(object sender, EventArgs e)
        {
            lock (Global.lockInternalData)
            {
                if (boolYTVideos.Checked) { Global.allowYTVirtualWatch = true; } else { Global.allowYTVirtualWatch = false; }
            }
        }

        private void boolVoiceNotes_CheckedChanged(object sender, EventArgs e)
        {
            lock (Global.lockInternalData)
            {
                if (boolVoiceNotes.Checked) { Global.allowBotAudios = true; } else { Global.allowBotAudios = false; }
            }
        }

        private void boolDMs_CheckedChanged(object sender, EventArgs e)
        {
            lock (Global.lockInternalData)
            {
                if (boolDMs.Checked) { Global.loadedData.ALLOWDMS = true; } else { Global.loadedData.ALLOWDMS = false; }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://ko-fi.com/karstskarn";

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    MessageBox.Show("Unsupported OS platform 😿", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://github.com/KarstSkarn/ChAIScrapper";

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    MessageBox.Show("Unsupported OS platform 😿", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://github.com/KarstSkarn";

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    MessageBox.Show("Unsupported OS platform 😿", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://discord.com/invite/d9rNwkerZw";

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    MessageBox.Show("Unsupported OS platform 😿", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://karstskarn.carrd.co/";

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    MessageBox.Show("Unsupported OS platform 😿", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label42_Click(object sender, EventArgs e)
        {

        }
    }
}
