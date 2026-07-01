using System;

namespace HiGCentrifugeInterface
{
    public class Callback_Wrapper
    {
        public void OnStateChanged()
        {
            Console.WriteLine("State changed in Hig4 BioNex Integration.");
        }
    }
}