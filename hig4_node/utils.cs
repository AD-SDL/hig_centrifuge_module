using Grapevine;
using System;
using System.Collections.Generic;

namespace hig4_node
{
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

    public static class UtilityFunctions
    {
        public static Dictionary<string, string> step_result(string action_response = StepStatus.IDLE, string action_msg = "", string action_log = "")
        {
            Dictionary<string, string> response = new Dictionary<string, string>();
            response["action_response"] = action_response;
            response["action_msg"] = action_msg;
            response["action_log"] = action_log;
            return response;
        }

        public static Dictionary<string, string> step_succeeded(string result = "")
        {
            return step_result(result);
        }

        public static Dictionary<string, string> step_failed(string reason = "")
        {
            Console.WriteLine(reason);
            return step_result(action_response: StepStatus.FAILED, action_log: reason);
        }

        public static void updateModuleStatus(IRestServer server, string status)
        {
            server.Locals.TryUpdate("state", status, server.Locals.GetAs<string>("state"));
        }

    }

}
