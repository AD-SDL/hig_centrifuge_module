using BioNex.HiGIntegration;
using Grapevine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hig4_node
{
    [RestResource]
    public class Hig4RestServer
    {
        private readonly IRestServer _server;

        public Hig4RestServer(IRestServer server)
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
            await context.Response.SendResponseAsync(JsonConvert.DeserializeObject(@"
                {
                    ""name"":""HiG4 Centrifuge"",
                    ""model"":""BioNex HiG 4 Automated Centrifuge"",
                    ""interface"":""wei_rest_node"",
                    ""version"":""0.1.0"",
                    ""description"":""Module for automating the HiG 4 Automated Centrifuge instrument."",
                    ""actions"": [
                        {
                            ""name"":""spin"",
                            ""args"":[
                                {
                                    ""name"":""gs"",
                                    ""type"":""double"",
                                    ""default"":null,
                                    ""required"":true,
                                    ""description"":""The number of G's to spin the sample at for the duration.""
                                },
                                {
                                    ""name"":""accel_percent"",
                                    ""type"":""double"",
                                    ""default"":null,
                                    ""required"":true,
                                    ""description"":""How quickly to accelerate the centrifuge up to speed.""
                                },
                                {
                                    ""name"":""decel_percent"",
                                    ""type"":""double"",
                                    ""default"":null,
                                    ""required"":true,
                                    ""description"":""How quickly to decelrate the centrifuge at the end of the spin.""
                                },
                                {
                                    ""name"":""time_seconds"",
                                    ""type"":""double"",
                                    ""default"":null,
                                    ""required"":true,
                                    ""description"":""The time in seconds to spin the sample for.""
                                },
                            ],
                            ""files"":[]
                        },
                        {
                            ""name"":""open_shield"",
                            ""args"":[
                                {
                                    ""name"":""bucket_index"",
                                    ""type"":""int"",
                                    ""default"":null,
                                    ""required"":true,
                                    ""description"":""Which bucket to present when opening the shield.""
                                },
                            ],
                            ""files"":[]
                        },
                        {
                            ""name"":""close_shield"",
                            ""args"":[],
                            ""files"":[]
                        },
                        {
                            ""name"":""home"",
                            ""args"":[],
                            ""files"":[]
                        },
                        {
                            ""name"":""abort_spin"",
                            ""args"":[],
                            ""files"":[]
                        },
                    ],
                    ""resource_pools"":[]
                }").ToString()
            );
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
                result = UtilityFunctions.step_result(StepStatus.FAILED, "", "Module is Busy");
                await context.Response.SendResponseAsync(JsonConvert.SerializeObject(result));
            }

            UtilityFunctions.updateModuleStatus(_server, ModuleStatus.BUSY);
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
                        result = UtilityFunctions.step_succeeded(
                            $"Spun for {args["time_seconds"]} at {args["gs"]} with acceleration percentage of {args["accel_percent"]} and deceleration percentage of {args["decel_percent"]}"
                        );
                        break;
                    case "open_shield":
                        hig_interface.OpenShield(
                            Int32.Parse(args["bucket_index"])
                        );
                        result = UtilityFunctions.step_succeeded($"Opened Shield and presented bucket {args["bucket_index"]}");
                        break;
                    case "home":
                        hig_interface.Home();
                        result = UtilityFunctions.step_succeeded("Homed Hig4");
                        break;
                    case "close_shield":
                        hig_interface.CloseShield();
                        result = UtilityFunctions.step_succeeded("Closed Shield");
                        break;
                    case "abort_spin":
                        hig_interface.AbortSpin();
                        result = UtilityFunctions.step_succeeded("Spin Aborted");
                        break;
                    default:
                        result = UtilityFunctions.step_failed("Unknown action: " + action_handle);
                        break;
                }
                UtilityFunctions.updateModuleStatus(_server, ModuleStatus.IDLE);
            }
            catch (Exception ex)
            {
                UtilityFunctions.updateModuleStatus(_server, ModuleStatus.ERROR);
                result = UtilityFunctions.step_failed("Step failed: " + ex.ToString());
            }

            await context.Response.SendResponseAsync(JsonConvert.SerializeObject(result));
        }
    }
}
