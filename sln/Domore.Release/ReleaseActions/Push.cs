using System.IO;

namespace Domore.ReleaseActions {
    class Push : ReleaseAction {
        public string Source { get; set; }

        public override void Work() {
            foreach (var pkg in new[] { "Domore.Configuration", "Domore.Configuration.ConfigurationManager" }) {
                var pkgFile = Path.Combine(SolutionDirectory, $"{pkg}.{Context.Version.StagedVersion}.nupkg");
                var pkgSource = Source?.Trim() ?? "";
                Process("nuget", "push", $"\"{pkgFile}\"", "-Source", pkgSource == "" ? "nuget.org" : pkgSource);
            }
        }
    }
}
