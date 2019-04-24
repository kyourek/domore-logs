using System;

namespace Domore {
    using Logs.Sample;

    class Program {
        static void Main(string[] args) {
            new Sample().Run();
            Console.WriteLine("[Enter] to exit");
            Console.ReadLine();
        }
    }
}
