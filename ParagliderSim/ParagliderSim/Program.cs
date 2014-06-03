using System;

namespace ParagliderSim
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (ParagliderSimulator game = new ParagliderSimulator())
            {
                game.Run();
            }
        }
    }
#endif
}

