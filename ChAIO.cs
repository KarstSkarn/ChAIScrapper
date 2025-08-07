using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChAIScrapperWF
{
    public static class ChAIO
    {
        public static TextBox textBoxGeneral;
        public static TextBox textBoxDiscord;
        public static TextBox textBoxScrapper;

        public static void WriteGeneral(string text)
        {
            lock (Global.lockGeneralWrite)
            {
                try
                {
                    DateTime currentTime = DateTime.Now;
                    string formattedTimeString = $"\n [ {currentTime:dd/MM/yyyy} - {currentTime:HH:mm:ss} ]";
                    if (textBoxGeneral.InvokeRequired)
                    {
                        textBoxGeneral.Invoke(new Action(() =>
                        {
                            textBoxGeneral.AppendText(formattedTimeString + " " + text + Environment.NewLine);
                        }));
                    }
                    else
                    {
                        textBoxGeneral.AppendText(formattedTimeString + " " + text + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "HEHE", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }
            }
        }

        public static void WriteDiscord(string text)
        {
            lock (Global.lockDiscordWrite)
            {
                try
                {
                    DateTime currentTime = DateTime.Now;
                    string formattedTimeString = $"\n [ {currentTime:dd/MM/yyyy} - {currentTime:HH:mm:ss} ]";
                    if (textBoxDiscord.InvokeRequired)
                    {
                        textBoxDiscord.Invoke(new Action(() =>
                        {
                            textBoxDiscord.AppendText(formattedTimeString + " " + text + Environment.NewLine);
                        }));
                    }
                    else
                    {
                        textBoxDiscord.AppendText(formattedTimeString + " " + text + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "HEHE", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }
            }
        }

        public static void WriteScrapper(string text)
        {
            lock (Global.lockScrapperWrite)
            {
                try
                {
                    DateTime currentTime = DateTime.Now;
                    string formattedTimeString = $"\n [ {currentTime:dd/MM/yyyy} - {currentTime:HH:mm:ss} ]";
                    if (textBoxScrapper.InvokeRequired)
                    {
                        textBoxScrapper.Invoke(new Action(() =>
                        {
                            textBoxScrapper.AppendText(formattedTimeString + " " + text + Environment.NewLine);
                        }));
                    }
                    else
                    {
                        textBoxScrapper.AppendText(formattedTimeString + " " + text + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "HEHE", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }
            }
        }
    }
}
