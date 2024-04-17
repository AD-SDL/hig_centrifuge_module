#!/usr/bin/env python3

from pathlib import Path

from wei import ExperimentClient


def main():
    # The path to the Workflow definition yaml file
    wf_path = Path("./test_hig_workflow.yaml")
    # This defines the Experiment object that will communicate with the server for workflows
    exp = ExperimentClient("127.0.0.1", "8000", "Test_HiG_Application")
    # This initializes the connection to the server and the logs for this run of the program.
    exp.register_exp()
    # This runs the workflow and returns once the workflow is complete
    exp.start_run(wf_path.resolve(), simulate=False, blocking=True)


if __name__ == "__main__":
    main()
