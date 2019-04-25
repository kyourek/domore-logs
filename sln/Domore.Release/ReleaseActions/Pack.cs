using System.IO;

namespace Domore.ReleaseActions {
    class Pack : ReleaseAction {
        public override void Work() {
            foreach (var nuspec in new[] { "Domore.Configuration", "Domore.Configuration.ConfigurationManager" }) {
                Log("Packing " + nuspec + "...");

                var nuspecFile = Path.Combine(SolutionDirectory, nuspec + ".nuspec");
                Log("Path: " + nuspecFile);

                Process("nuget", "pack", $"\"{nuspecFile}\"", "-OutputDirectory", $"\"{SolutionDirectory}\"");
            }
        }
    }
}
