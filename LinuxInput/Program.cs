using LinuxInputs.Inputs;
using System;

namespace LinuxInputs
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            InputsManager.OnKeyDown += InputsManager_OnKeyDown;
            InputsManager.OnKeyUp += InputsManager_OnKeyUp;

            Console.Read();
        }

        private static void InputsManager_OnKeyDown(object sender, InputKeyEventCode e)
        {
            Console.WriteLine($"{e} pushed");
        }

        private static void InputsManager_OnKeyUp(object sender, InputKeyEventCode e)
        {
            Console.WriteLine($"{e} released");
        }
    }
}
