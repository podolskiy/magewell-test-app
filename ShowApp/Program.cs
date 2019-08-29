using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExternalAPI.Magewell;

namespace ShowApp
{
    static class Program
    {
        private static MagewellForm magewellForm;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += ApplicationOnThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            if (!MWCapture.Init())
            {
                MessageBox.Show($"Error starting plugin: MagewellVideowall");
                return;
            }
            var inputId = int.Parse(args[0]);
            var closeAfter = int.Parse(args[1]);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            magewellForm = new MagewellForm(inputId);

            ThreadPool.QueueUserWorkItem(state =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(closeAfter));
                magewellForm.Close();
            });

            Application.Run(magewellForm);
        }

        private static void ApplicationOnThreadException(object o, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"Top-level thread error.{e.Exception.Message}");
        }

        static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Top-level domain error. {((Exception)e.ExceptionObject).Message}");
        }
    }
}
