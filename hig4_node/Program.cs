using BioNex.HiGIntegration;
using Grapevine;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace hig4_node
{
    public class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Server Port")]
        public int Port { get; } = 2005;

        private void OnExecute()
        {
            HiGInterface hiG = new HiG();
            using (var server = RestServerBuilder.UseDefaults().Build())
            {
                server.Prefixes.Add("http://localhost:" + Port.ToString() + "/");
                server.Locals.TryAdd("hig_interface", hiG);
                server.Start();

                Console.WriteLine("Press enter to stop the server");
                Console.ReadLine();
                server.Stop();
            }
        }
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
            HiGInterface hig = _server.Locals.GetAs<HiGInterface>("hig_interface");
            Dictionary<string, string> result = action_response();
            // TODO
            await context.Response.SendResponseAsync(
                "state"
            );
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
            await context.Response.SendResponseAsync("resource");
        }

        [RestRoute("Post", "/action")]
        public async Task Action(IHttpContext context)
        {
            String action_handle;
            String action_vars;
            Dictionary<string, string> args;
            Dictionary<string, string> result = action_response();

            HiGInterface hig = _server.Locals.GetAs<HiGInterface>("hig_interface");
            context.Request.PathParameters.TryGetValue("action_handle", out action_handle);
            context.Request.PathParameters.TryGetValue("action_vars", out action_vars);
            args = JsonConvert.DeserializeObject(action_vars) as Dictionary<string, string>;

            // TODO
            switch(action_handle)
            {
                case "spin":
                    //TODO
                    break;
                default:
                    Console.WriteLine("Unknown action: " +  action_handle);
                    result = action_response(StepStatus.FAILED, "", "Unknown action: " + action_handle);
                    break;
            }

            byte[] msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));
            await context.Response.SendResponseAsync(msg);
        }
    }
}
