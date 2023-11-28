﻿using BioNex.HiGIntegration;
using Grapevine;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        }

        ~HiGNode()
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



    [RestResource]
    public class Hig4ModuleRestServer
    {
        private readonly IRestServer _server;

        public Hig4ModuleRestServer(IRestServer server)
        {
            _server = server;
        }

        [RestRoute("Get", "/state")]
        public async Task State(IHttpContext context)
        {
            string state = _server.Locals.GetAs<string>("state");
            Dictionary<string, string> response = new Dictionary<string, string>
            {
                ["State"] = state
            };
            await context.Response.SendResponseAsync(JsonConvert.SerializeObject(response));
        }

        [RestRoute("Get", "/about")]
        public async Task About(IHttpContext context)
        {
            // TODO
            await context.Response.SendResponseAsync("about");
        }

        [RestRoute("Get", "/resources")]
        public async Task Resources(IHttpContext context)
        {
            // TODO
            await context.Response.SendResponseAsync("resources");
        }

        [RestRoute("Post", "/action")]
        public async Task Action(IHttpContext context)
        {
            Dictionary<string, string> result;

            HiGInterface hig_interface = _server.Locals.GetAs<HiGInterface>("hig_interface");
            string state = _server.Locals.GetAs<string>("state");
            string action_handle = context.Request.QueryString["action_handle"];
            string action_vars = context.Request.QueryString["action_vars"];
            Dictionary<string, string> args = JsonConvert.DeserializeObject<Dictionary<string, string>>(action_vars);

            if (state == ModuleStatus.BUSY)
            {
                result = UtilityFunctions.StepResponse(StepStatus.FAILED, "", "Module is Busy");
                await context.Response.SendResponseAsync(JsonConvert.SerializeObject(result));
            }

            _server.Locals.TryUpdate("state", ModuleStatus.BUSY, _server.Locals.GetAs<string>("state"));
            try
            {
                switch (action_handle)
                {
                    case "spin":
                        hig_interface.Spin(
                            double.Parse(args["gs"]),
                            double.Parse(args["accel_percent"]),
                            double.Parse(args["decel_percent"]),
                            double.Parse(args["time_seconds"])
                        );
                        result = UtilityFunctions.StepResponse(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "open_shield":
                        hig_interface.OpenShield(
                            Int32.Parse(args["bucket_index"])
                        );
                        result = UtilityFunctions.StepResponse(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "home":
                        hig_interface.Home();
                        result = UtilityFunctions.StepResponse(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "close_shield":
                        hig_interface.CloseShield();
                        result = UtilityFunctions.StepResponse(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "abort_spin":
                        hig_interface.AbortSpin();
                        result = UtilityFunctions.StepResponse(StepStatus.SUCCEEDED, "", "");
                        break;
                    default:
                        Console.WriteLine("Unknown action: " + action_handle);
                        result = UtilityFunctions.StepResponse(StepStatus.FAILED, "", "Unknown action: " + action_handle);
                        break;
                }
                _server.Locals.TryUpdate("state", ModuleStatus.IDLE, _server.Locals.GetAs<string>("state"));
            }
            catch (Exception ex)
            {
                _server.Locals.TryUpdate("state", ModuleStatus.ERROR, _server.Locals.GetAs<string>("state"));
                Console.WriteLine(ex.ToString());
                result = UtilityFunctions.StepResponse(StepStatus.FAILED, "", "Step failed: " + ex.ToString());
            }

            await context.Response.SendResponseAsync(JsonConvert.SerializeObject(result));
        }
    }
}
