using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ExternalAPI.Magewell;
using Magewell;

namespace ManageApp
{
    class Program
    {
        private static MagewellInputsUpdater inputsUpdater;
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += ApplicationOnThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            try
            {
                if (!MWCapture.Init())
                {
                    Console.WriteLine($"Error starting plugin: MagewellVideowall");
                    return;
                }
                // every 5 sec updates list of files in inputs folders 
                inputsUpdater = new MagewellInputsUpdater("Magewell", "vmfu");
                inputsUpdater.Init();

                ThreadPool.QueueUserWorkItem(state => RunManager());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }

        private static void RunManager()
        {
            try
            {
                var inputs = inputsUpdater.GetInputs()
                    .Where(input => input.Available)
                    .GroupBy(input => input.OrderId)
                    .Select(g => g.First())
                    .ToArray();

                while (true)
                {

                    foreach (var input in inputs)
                    {
                        StartApp(input,2);
                    }



                    Thread.Sleep(15 * 1000);
                }
            }
            catch (Exception e)
            {

            }
        }

        private static void StartApp(Input input, int processNum)
        {
            for (int i = 0; i < processNum; i++)
            {
                Process process = new Process
                {
                    StartInfo =
                    {
                        FileName = "ShowApp.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        Arguments = $"{input.OrderId} {15}"
                    }
                };

                process.Start();
            }
        }



        private static void ApplicationOnThreadException(object o, ThreadExceptionEventArgs e)
        {
            Console.WriteLine($"Top-level thread error.{e.Exception.Message}");
        }

        static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"Top-level domain error. {((Exception)e.ExceptionObject).Message}");
        }
    }
}
