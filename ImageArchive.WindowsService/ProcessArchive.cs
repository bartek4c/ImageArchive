using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageArchive.WindowsService
{
    public partial class ProcessArchive : ServiceBase
    {
        private Thread m_Thread;
        private ManualResetEvent m_StopSignal = new ManualResetEvent(false);
        private ImageArchive.Processor.Processor _processor;

        public ProcessArchive()
        {
            _processor = new ImageArchive.Processor.Processor();
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            m_Thread = new Thread(Run);
            m_Thread.Start();
        }

        protected override void OnStop()
        {
            m_StopSignal.Set();
            if (!m_Thread.Join(600000))
            {
                m_Thread.Abort(); // Abort as a last resort.
            }
        }

        private void Run()
        {
            //entry point to the processr from windows service - every 5 minutes
            //while (!m_StopSignal.WaitOne(300000))
            while (!m_StopSignal.WaitOne(6000))
            {
                Console.WriteLine("Start Processing: " + DateTime.Now.ToLongTimeString());
                _processor.RunProcessor();
                Console.WriteLine("Finished: " + DateTime.Now.ToLongTimeString());
            }
        }
    }
}
