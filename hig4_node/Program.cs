using BioNex.HiGIntegration;
using Grapevine;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hig4_node
{
    public class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Server Port")]
        public int Port { get; } = 2005;
        public string state = ModuleStatus.INIT;
        private HiGInterface hig_interface = new HiG();
        private IRestServer server;
        //private string error_reason = "";

        private void OnExecute()
        {
            InitializeHiG();
            server = RestServerBuilder.UseDefaults().Build();
            server.Prefixes.Add("http://localhost:" + Port.ToString() + "/");
            server.Locals.TryAdd("hig_interface", hig_interface);
            server.Locals.TryAdd("state", state);
            server.Start();

            Console.WriteLine("Press enter to stop the server");
            Console.ReadLine();
            server.Stop();
            hig_interface.Close();
        }

        private void InitializeHiG()
        {
            hig_interface.Blocking = true;

            // The next section is important – if the HiG is operated in non-blocking (asynchronous) mode (Blocking = false), 
            // then it is necessary to register the following events so that your application will know when an operation is completed. 
            // If you are running in blocking mode (Blocking = true), then you do not need to register these event handlers. 
            // The handlers below i.e. hig_InitializeComplete, hig_InitializeError, etc., are not outlined in this example, 
            // but they have the standard event handler signature, e.g: func_name(object, EventArgs). 
            // EventArgs can be cast to a BioNex.HiGIntegration.HiG.ErrorEventArgs object in order to access the Reason property (the reason for the error). 
            // If running in non-blocking mode (Blocking = false), then you need to wrap the API function call in a try / catch block.

            //hig_interface.InitializeComplete += new EventHandler(hig_InitializeComplete);
            //hig_interface.InitializeError += new EventHandler(hig_HandleError);
            //hig_interface.HomeComplete += new EventHandler(hig_ActionComplete);
            //hig_interface.HomeError += new EventHandler(hig_HandleError);
            //hig_interface.OpenShieldComplete += new EventHandler(hig_ActionComplete);
            //hig_interface.OpenShieldError += new EventHandler(hig_HandleError);
            //hig_interface.SpinComplete += new EventHandler(hig_ActionComplete);
            //hig_interface.SpinError += new EventHandler(hig_HandleError);

            string id = "0"; // You can determine the ID of the HiG by running the USB-CANmodul Utility and using the value in the "DevNr" column
            string device_name = "HiG4 Centrifuge " + id; // Change this name to whatever you want to call it. This name is used in the log file.
            bool simulate = true; // Set to false if you are connected to actual HiG hardware, or true if you'd like to simulate a HiG
            hig_interface.Initialize(device_name, id, simulate); // again, if Blocking = true, Initialize will block until it has completed or has experienced an error. You must wrap the call
        }

        //public void hig_InitializeComplete(object sender, EventArgs e)
        //{
        //    state = ModuleStatus.IDLE;
        //    server.Locals.TryUpdate("state", state, server.Locals.GetAs<string>("state"));
        //}

        //private void hig_HandleError(object sender, EventArgs e)
        //{
        //    HiG.ErrorEventArgs event_args = (HiG.ErrorEventArgs) e;
        //    error_reason = event_args.Reason;
        //    state = ModuleStatus.ERROR;
        //    server.Locals.TryUpdate("state", state, server.Locals.GetAs<string>("state"));
        //}

        //private void hig_ActionComplete(object sender, EventArgs e)
        //{
        //    state = ModuleStatus.IDLE;
        //    server.Locals.TryUpdate("state", state, server.Locals.GetAs<string>("state"));
        //}

    }

    public static class ModuleStatus
    {
        public const string
            INIT = "INIT",
            IDLE = "IDLE",
            BUSY = "BUSY",
            ERROR = "ERROR",
            UNKNOWN = "UNKNOWN";
    }

    public static class StepStatus
    {
        public const string
            IDLE = "idle",
            RUNNING = "running",
            SUCCEEDED = "succeeded",
            FAILED = "failed";
    }



    [RestResource]
    public class Hig4ModuleRestServer
    {
        private IRestServer _server;

        private Dictionary<string, string> action_response(string action_response = "", string action_msg = "", string action_log = "")
        {
            Dictionary<string, string> response = new Dictionary<string, string>();
            response["action_response"] = action_response;
            response["action_msg"] = action_msg;
            response["action_log"] = action_log;
            return response;
        }

        public Hig4ModuleRestServer(IRestServer server)
        {
            _server = server;
        }

        [RestRoute("Get", "/state")]
        public async Task State(IHttpContext context)
        {
            string state = _server.Locals.GetAs<string>("state");
            Dictionary<string, string> response = new Dictionary<string, string>();
            response["State"] = state;
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
            string action_handle;
            string action_vars;
            Dictionary<string, string> args;
            Dictionary<string, string> result = action_response();

            HiGInterface hig_interface = _server.Locals.GetAs<HiGInterface>("hig_interface");
            string state = _server.Locals.GetAs<string>("state");
            context.Request.PathParameters.TryGetValue("action_handle", out action_handle);
            context.Request.PathParameters.TryGetValue("action_vars", out action_vars);
            args = JsonConvert.DeserializeObject(action_vars) as Dictionary<string, string>;

            if (state == ModuleStatus.BUSY)
            {
                result = action_response(StepStatus.FAILED, "", "Module is Busy");
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
                        result = action_response(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "open_shield":
                        hig_interface.OpenShield(
                            Int32.Parse(args["bucket_index"])
                        );
                        result = action_response(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "home_shield":
                        hig_interface.HomeShield(
                            bool.Parse(args["open_shield_after_home_complete"])
                        );
                        result = action_response(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "home":
                        hig_interface.Home();
                        result = action_response(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "close_shield":
                        hig_interface.CloseShield();
                        result = action_response(StepStatus.SUCCEEDED, "", "");
                        break;
                    case "abort_spin":
                        hig_interface.AbortSpin();
                        result = action_response(StepStatus.SUCCEEDED, "", "");
                        break;
                    default:
                        Console.WriteLine("Unknown action: " + action_handle);
                        result = action_response(StepStatus.FAILED, "", "Unknown action: " + action_handle);
                        break;
                }
                _server.Locals.TryUpdate("state", ModuleStatus.IDLE, _server.Locals.GetAs<string>("state"));
            }
            catch (Exception ex)
            {
                _server.Locals.TryUpdate("state", ModuleStatus.ERROR, _server.Locals.GetAs<string>("state"));
                Console.WriteLine(ex.ToString());
                result = action_response(StepStatus.FAILED, "", "Step failed: " + ex.ToString());
            }

            await context.Response.SendResponseAsync(JsonConvert.SerializeObject(result));
        }
    }
}
