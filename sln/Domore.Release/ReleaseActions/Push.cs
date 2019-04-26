using System.Collections.Generic;
using System.IO;

namespace Domore.ReleaseActions {
    class Push : ReleaseAction {
        public string Source { get; set; }
        public IDictionary<string, string> Project { get; set; } = new Dictionary<string, string>();

        public override void Work() {
            foreach (var project in Project.Values) {
                var pkgDir = Path.Combine(SolutionDirectory, project, "bin", "Release");
                var pkgFile = Path.Combine(pkgDir, $"{project}.{Context.Version.StagedVersion}.nupkg");
                var pkgSource = Source?.Trim() ?? "";
                Process("nuget", "push", $"\"{pkgFile}\"", "-Source", pkgSource == "" ? "nuget.org" : pkgSource);
            }
        }
    }
}
