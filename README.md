# LinuxInputs

Just a little project to handle inputs keys events management in linux (keys up/down for keyboard, pad, remote controller).
The goal was to support a remote controller in a dotnet app on raspberry pi.

The class detects (dis)connections of devices (hotplugs) and listens automatically to them.

The mapped keys enum is from linux kernel source : https://git.kernel.org/pub/scm/linux/kernel/git/torvalds/linux.git/tree/include/uapi/linux/input-event-codes.h

## Contributors

- Kevin "kipy" Piette