# ProxifierDrvAutoToggle
Auto-toggle Proxifier's driver for convenience against EA's Javelin Anti-Cheat.

tl;dr: This tool ensures Proxifier is completely disabled when you exit the Proxifier program (without needing to uninstall Proxifier), in order to let you play [Battlefield 6](https://store.steampowered.com/app/2807960/Battlefield_6/) more conveniently.

This is a very bare-bone workaround.

# Usage
## This is a service-based tool. Meaning you need to **install** the tool as service and let it run in background.
- Use launch argument `install` to install the tool as a background service.
- Use launch argument `uninstall` (or `remove`, `delete`) to uninstall the background service which has been created by this tool.

### But why running background?
- The tool needs to monitor whether *Proxifier's front-end GUI* is running or not. When *the Proxifier's GUI* isn't running, the tool will automatically stop the `ProxifierDrv` (the back-bone driver of Proxifier).

# Note
## When `ProxifierDrv` is stopped, you **may** need to run *the Proxifier's GUI* **TWICE** within 5-second.
### Because the first time you run *Proxifier's GUI* will pop up an error dialog complaining about "Proxifier driver is not running". Just close the error dialog and immediately run *the Proxifier's GUI* again within 5-second (countdown start when the error dialog appears) and it should be working.
#### If you run Proxifier but the error dialog doesn't show up, then it's all good on first try.
Why this happens is because this tool **may** not start the `ProxifierDrv` service fast enough to be before *the Proxifier's GUI* launches.
