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
    //public struct ActionResponse
    //{
    //    //TODO: Convert to class?
    //    String action_response;
    //    String action_msg;
    //    String action_log;
    //}

    [RestResource]
    public class Hig4ModuleRestServer
    {
        private IRestServer _server;

        public Hig4ModuleRestServer(IRestServer server)
        {
            _server = server;
        }

        [RestRoute("Get", "/state")]
        public async Task State(IHttpContext context)
        {
            HiGInterface hig = _server.Locals.GetAs<HiGInterface>("hig_interface");
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
            Dictionary<string, string> response;
            //ActionResponse action_response;


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
                    Console.WriteLine("Unknown action:" +  action_handle);
                    response = new Dictionary<string, string>();
                    response.Add("action_response", "StepStatus.SUCCEEDED");
                    response.Add("action_msg", "block free");
                    response.Add("action_log", "block free");
                    msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
                    break;
            }

            await context.Response.SendResponseAsync(args);
        }
    }
}
