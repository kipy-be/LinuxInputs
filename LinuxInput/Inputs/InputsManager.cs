using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace LinuxInputs.Inputs
{
    public static class InputsManager
    {
        private static string INPUT_PATH = "/dev/input";
        private static string INPUT_BY_ID_PATH = Path.Combine(INPUT_PATH, "by-id");
        private static int CHECK_DEVICE_DEBOUNCE_VALUE = 2000;

        private static FileSystemWatcher _devicesWatcher;
        private static object _lock = new object();
        private static Dictionary<string, bool> _listenedDevices = new Dictionary<string, bool>();
        private static Timer _checkDevicesDebounceTimer;

        public static event EventHandler<InputKeyEventCode> OnKeyDown;
        public static event EventHandler<InputKeyEventCode> OnKeyUp;

        static InputsManager()
        {
            Debug.WriteLine("InputKeysManager: init");

            CheckDevices();
            SetupWatcher();
        }

        private static void SetupWatcher()
        {
            try
            {
                _checkDevicesDebounceTimer = new Timer(CHECK_DEVICE_DEBOUNCE_VALUE);
                _checkDevicesDebounceTimer.Elapsed += CheckDevicesDebouceTimerElapsed;
                _checkDevicesDebounceTimer.AutoReset = false;
                _checkDevicesDebounceTimer.Enabled = false;

                _devicesWatcher = new FileSystemWatcher(INPUT_PATH);
                _devicesWatcher.Created += OnInputCreated;

                _devicesWatcher.IncludeSubdirectories = true;
                _devicesWatcher.EnableRaisingEvents = true;
            }
            catch
            {
                Debug.WriteLine("InputsManager: Could not set a file system watcher on {0}", INPUT_PATH);
            }
        }

        public static void Dispose()
        {
            if (_devicesWatcher != null)
            {
                _devicesWatcher.Created -= OnInputCreated;

                _devicesWatcher.Dispose();
                _devicesWatcher = null;
            }
        }

        private static bool IsKeysInput(string inputName) => inputName.EndsWith("-kbd");

        private static void OnInputCreated(object sender, FileSystemEventArgs e)
        {
            _checkDevicesDebounceTimer.Stop();
            _checkDevicesDebounceTimer.Start();
        }

        private static void CheckDevicesDebouceTimerElapsed(object sender, ElapsedEventArgs e) => CheckDevices();

        private static void CheckDevices()
        {
            try
            {
                var di = new DirectoryInfo(INPUT_BY_ID_PATH);
                foreach (var device in di.EnumerateFiles())
                {
                    if (IsKeysInput(device.Name))
                    {
                        ListenToInputKeysEvents(device.FullName);
                    }
                }
            }
            catch {}
        }

        private static void ListenToInputKeysEvents(string dev)
        {
            lock(_lock)
            {
                if (_listenedDevices.ContainsKey(dev))
                {
                    return;
                }

                _listenedDevices.Add(dev, true);
            }

            Task.Run(() =>
            {
                try
                {
                    Debug.WriteLine($"InputsManager: Now listening to {dev}");

                    using (var stream = new FileStream(dev, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        byte[] buffer = new byte[24];

                        InputKeyEventType type;
                        InputKeyEventCode code;
                        uint value;

                        while (true)
                        {
                            stream.Read(buffer, 0, buffer.Length);

                            type = (InputKeyEventType)((buffer[17] << 8) | buffer[16]);
                            code = (InputKeyEventCode)((buffer[19] << 8) | buffer[18]);
                            value = (uint)((buffer[23] << 24)
                                | (buffer[22] << 16)
                                | (buffer[21] << 8)
                                | buffer[20]);

                            if (type == InputKeyEventType.EV_KEY)
                            {
                                if (value == 1)
                                {
                                    OnKeyDown?.Invoke(null, code);
                                }
                                else if (value == 0)
                                {
                                    OnKeyUp?.Invoke(null, code);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    Debug.WriteLine($"InputsManager: Finished listening to {dev}");

                    lock (_lock)
                    {
                        _listenedDevices.Remove(dev);
                    }
                }
            });
        }
    }
}
