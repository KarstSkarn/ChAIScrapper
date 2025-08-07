using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChAIScrapperWF
{
    public static class ChAIAppend
    {
        static object saveLock = new object();
        static object loadLock = new object();
        public static void SaveObjectToFile<T>(T objectToSave, string filePath)
        {
            const int maxRetries = 100;
            const int minDelayMilliseconds = 20;
            const int maxDelayMilliseconds = 40;

            int retries = 0;
            Random random = new Random();

            while (retries < maxRetries)
            {
                try
                {
                    lock (saveLock)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        using (TextWriter writer = new StreamWriter(filePath))
                        {
                            serializer.Serialize(writer, objectToSave);
                        }
                    }

                    // Save successful, exit the loop
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    retries++;
                    int delayMilliseconds = random.Next(minDelayMilliseconds, maxDelayMilliseconds + 1);
                    System.Threading.Thread.Sleep(delayMilliseconds);
                    //Global.AppendToFile("ErrorLog.txt", ex.ToString());
                }
            }
        }
        public static T LoadObjectFromFile<T>(string filePath)
        {
            const int maxRetries = 100;
            const int minDelayMilliseconds = 20;
            const int maxDelayMilliseconds = 40;

            int retries = 0;
            Random random = new Random();

            while (retries < maxRetries)
            {
                try
                {
                    lock (loadLock)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        using (TextReader reader = new StreamReader(filePath))
                        {
                            return (T)serializer.Deserialize(reader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    retries++;
                    int delayMilliseconds = random.Next(minDelayMilliseconds, maxDelayMilliseconds + 1);
                    System.Threading.Thread.Sleep(delayMilliseconds);
                    //Global.AppendToFile("ErrorLog.txt", ex.ToString());
                }
            }
            return default(T);
        }
    }
}
