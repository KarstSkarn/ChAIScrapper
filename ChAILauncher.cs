using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChAIScrapperWF
{
    public static class ChAILauncher
    {
        public static async Task MonitorTasks(CancellationTokenSource cancellationTokenSource)
        {
            // Función local para iniciar las tareas
            async Task<(Task scraping, Task discord)> StartTasks(CancellationTokenSource cts)
            {
                var scrapingTask = Task.Run(() => ChAIWebScrapper.RunWebScraper(cts)); // <-- Pasamos el CTS completo
                ChAIO.WriteGeneral("Web Scrapping task started!");

                var discordBotTask = ChAIDiscordBot.RunDiscordBotAsync(cts); // <-- Igual aquí
                ChAIO.WriteGeneral("Discord Bot task started!");

                return (scrapingTask, discordBotTask);
            }

            var (scrapingTask, discordBotTask) = await StartTasks(cancellationTokenSource);

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (scrapingTask.IsCompleted)
                {
                    if (scrapingTask.IsFaulted)
                    {
                        ChAIO.WriteGeneral($"Scraping task faulted: {scrapingTask.Exception}");
                    }

                    scrapingTask = Task.Run(() => ChAIWebScrapper.RunWebScraper(cancellationTokenSource));
                    ChAIO.WriteGeneral("Scraping task restarted.");
                }

                if (discordBotTask.IsCompleted)
                {
                    if (discordBotTask.IsFaulted)
                    {
                        ChAIO.WriteGeneral($"Discord bot task faulted: {discordBotTask.Exception}");
                    }

                    discordBotTask = ChAIDiscordBot.RunDiscordBotAsync(cancellationTokenSource);
                    ChAIO.WriteGeneral("Discord bot task restarted.");
                }

                // 🚨 Soft Reset
                if (Global.programSoftResetFlag)
                {
                    ChAIO.WriteGeneral("Soft reset requested. Cancelling tasks...");
                    Global.programSoftResetFlag = false;

                    cancellationTokenSource.Cancel();

                    try
                    {
                        await Task.WhenAll(scrapingTask, discordBotTask);
                    }
                    catch (Exception ex)
                    {
                        ChAIO.WriteGeneral($"Exception while waiting for tasks to cancel: {ex}");
                    }

                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();

                    (scrapingTask, discordBotTask) = await StartTasks(cancellationTokenSource);
                    ChAIO.WriteGeneral("Soft reset complete. Tasks relaunched.");
                }

                await Task.Delay(500);
            }

            await Task.WhenAll(scrapingTask, discordBotTask);
        }
    }
}
