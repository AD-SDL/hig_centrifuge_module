import json
import time
from typing import Dict, Any
import os as os

import clr
from System import AppDomain
from System.Reflection import Assembly
from madsci.common.types.action_types import (
    ActionCancelled,
    ActionSucceeded,
)
from madsci.common.types.admin_command_types import AdminCommandResponse
from madsci.common.types.node_types import (
    NodeIntrinsicLocationDefinition,
    NodeRepresentationTemplateDefinition,
    RestNodeConfig,
)
from madsci.node_module.helpers import action
from madsci.node_module.rest_node_module import RestNode
from pathlib import WindowsPath

clr.AddReference(
    str(
        WindowsPath(__file__).parent
        / "hig_centrifuge_interface"
        / "bin"
        / "Debug"
        / "HiGIntegration"
    )
)
from BioNex.HiGIntegration import HiG
from BioNex.HiGIntegration import HiGInterface

class HiGCentrifugeNodeConfig(RestNodeConfig):
    """Configuration for the HiGCentrifuge REST node"""

    device_id: int = 0
    device_name: str = "HiG4 Centrifuge"
    simulate: bool = True
    bionex_dir: str = r"C:\Program Files (x86)\BioNex\HiG"
    """Directory of the BioNex HiG install that holds the vendor DLLs
    (HiGIntegration.dll and its dependencies). Override per-machine if the
    driver software is installed somewhere else."""

class HiGCentrifugeNode(RestNode):

    hig_interface: HiGInterface = None
    config_model = HiGCentrifugeNodeConfig
    config: HiGCentrifugeNodeConfig = HiGCentrifugeNodeConfig()
    module_version = "0.8"

    def startup_handler(self) -> None:
        # Space for resource related stuff ***

        # The vendor's dependencies (CanDongleWrapper, TechnosoftLibraryMT, ...)
        # live in the BioNex install dir, which is not on the Python host's CLR
        # probe path. Register a resolver before Initialize() or it raises
        # FileNotFoundException for those assemblies -- even in simulate mode.
        self._register_vendor_assembly_resolver()

        self.hig_interface = HiG()
        self.hig_interface.Blocking = True
        self.hig_interface.Initialize(
            self.config.device_name,
            str(self.config.device_id),
            self.config.simulate
        )
        self.homed = False
        # self.logger.log("HiG Centrifuge initialized.")

    def _register_vendor_assembly_resolver(self) -> None:
        """Resolve BioNex managed + native deps from the configured install dir."""
        bionex_dir = self.config.bionex_dir
        if hasattr(os, "add_dll_directory") and os.path.isdir(bionex_dir):
            # Native (unmanaged) deps: CAN dongle / Technosoft motor drivers
            os.add_dll_directory(bionex_dir)

        def _resolve(sender, args):
            name = args.Name.split(",")[0].strip()
            dll = os.path.join(bionex_dir, name + ".dll")
            return Assembly.LoadFrom(dll) if os.path.isfile(dll) else None

        # Keep a reference so the delegate isn't garbage-collected.
        self._vendor_resolver = _resolve
        AppDomain.CurrentDomain.AssemblyResolve += _resolve

    def shutdown_handler(self) -> None:
        if self.hig_interface is not None:
            self.hig_interface.Close()
            self.hig_interface = None

    
    def state_handler(self) -> None:
        if not self.hig_interface:
            return
        # if not self.hig_interface.isHomed:
        #     self.node_state = {
        #         "hig4_status_code": "UNHOMED",
        #     }
        if self.hig_interface.Idle:
            self.node_state = {
                "hig4_status_code": "READY",
            }
        elif self.hig_interface.InErrorState:
            self.node_state = {
                "hig4_status_code": "ERRORED",
            }
        elif self.hig_interface.Busy:
            self.node_state = {
                "hig4_status_code": "BUSY",
            }
        else:
            self.node_state = {
                "hig4_status_code": "UNKNOWN",
            }

    @action
    def spin(
        self, gs, accel_percent, decel_percent, time_seconds
    ) -> None:
        self.hig_interface.Spin(gs, accel_percent, decel_percent, time_seconds)

    @action
    def open_shield(self, bucket_index) -> None:
        self.hig_interface.OpenShield(bucket_index)

    @action
    def home(self) -> None:
        self.hig_interface.Home()

    @action 
    def close_shield(self) -> None:
        self.hig_interface.CloseShield()

    @action
    def abort_spin(self) -> None:
        self.hig_interface.AbortSpin()

if __name__ == "__main__":
    hig_node = HiGCentrifugeNode()
    hig_node.start_node()

    