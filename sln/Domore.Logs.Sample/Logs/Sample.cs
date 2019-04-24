namespace Domore.Logs.Sample {
    class Sample {
        public void Run() {
            new Calculator().Add(1, 2);
        }
    }

    class Calculator {
        readonly ILog Log = Logging.For(typeof(Calculator));

        public void Add(int one, int two) {
            Log.Debug("Adding", one, two);
            Log.Info($"{one} + {two} = {one + two}");
            Log.Debug("Done");
        }
    }
}
