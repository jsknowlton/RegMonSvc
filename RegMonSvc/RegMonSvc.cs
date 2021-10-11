using System;
using System.ServiceProcess;
using Microsoft.Win32;

namespace RegMonSvc
{
    public partial class RegMonSvc : ServiceBase
    {
        private const string SubKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
        private static RegistryMonitor _monitor;

        public RegMonSvc()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string keyName = $@"{Registry.LocalMachine.Name}\{SubKey}";
            SetRegistryValues();
            _monitor = new RegistryMonitor(keyName);
            _monitor.RegChanged += OnRegChanged;
            _monitor.Start();

            Console.WriteLine("Monitor started");
        }

        protected override void OnStop()
        {
            _monitor.Stop();
            _monitor.RegChanged -= OnRegChanged;
            _monitor = null;
            Console.WriteLine("Monitor stopped");
        }

        private static void SetRegistryValues()
        {
            var policies = Registry.LocalMachine.OpenSubKey(SubKey, true);
            if (policies != null)
            {
                var enableLua = (int) policies.GetValue("EnableLUA", -1);
                if (enableLua > 0)
                {
                    policies.SetValue("EnableLUA", 0, RegistryValueKind.DWord);
                    //Console.WriteLine("Setting EnableLUA = 0");
                }

                var consentPromptBehaviorAdmin = (int) policies.GetValue("ConsentPromptBehaviorAdmin", -1);
                if (consentPromptBehaviorAdmin > 0)
                {
                    policies.SetValue("ConsentPromptBehaviorAdmin", 0, RegistryValueKind.DWord);
                    //Console.WriteLine("Setting ConsentPromptBehaviorAdmin = 0");
                }
            }
        }

        private static void OnRegChanged(object sender, EventArgs e)
        {
            Console.WriteLine("Registry change detected");
            SetRegistryValues();
        }
    }
}