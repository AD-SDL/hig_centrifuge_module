using BioNex.HiGIntegration;
using Grapevine;
using McMaster.Extensions.CommandLineUtils;
using System;

namespace hig4_node
{
    public class HiGNode
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<HiGNode>(args);

        [Option(Description = "Server Port")]
        public int Port { get; } = 2005;
        [Option(Description = "Device ID")]
        public int Id { get; } = 0;
        [Option(Description = "Device Name (for logging)")]
        public string Name { get; } = "HiG4 Centrifuge";
        [Option(Description = "Whether or not to simulate the device")]
        public bool Simulate { get; } = false;

        public string state = ModuleStatus.IDLE;
        private readonly HiGInterface hig_interface = new HiG();
        private IRestServer server;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by CommandLineApplication.Execute above")]
        private void OnExecute()
        {

            InitializeHiG();
            server = RestServerBuilder.UseDefaults().Build();
            server.Prefixes.Add("http://+:" + Port.ToString() + "/");
            server.Locals.TryAdd("hig_interface", hig_interface);
            server.Locals.TryAdd("state", state);
            server.Start();

            Console.WriteLine("Press enter to stop the server");
            Console.ReadLine();

            deconstruct();
        }

        private void deconstruct()
        {
            server.Stop();
            hig_interface.Close();
        }

        private void InitializeHiG()
        {
            hig_interface.Blocking = true;
            string id = Id.ToString(); // You can determine the ID of the HiG by running the USB-CANmodul Utility and using the value in the "DevNr" column
            string device_name = Name; // Change this name to whatever you want to call it. This name is used in the log file.
            hig_interface.Initialize(device_name, id, Simulate); // again, if Blocking = true, Initialize will block until it has completed or has experienced an error. You must wrap the call
        }
    }
}
