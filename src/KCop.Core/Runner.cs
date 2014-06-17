using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StyleCop;

namespace KCop.Core
{
    public class Runner
    {
        private string _projectFile;
        private readonly List<Violation> _violations;

        public Runner(string projectFile)
        {
            _projectFile = projectFile;
            _violations = new List<Violation>();

            OnViolationEncountered = Runner_ViolationEncountered;
            OnOutputGenerated = (obj, sender) => { };
        }

        public EventHandler<ViolationEventArgs> OnViolationEncountered { get; set; }
        public EventHandler<OutputEventArgs> OnOutputGenerated { get; set; }

        public List<Violation> Run()
        {
            var runner = new StyleCopConsole(
                settings: null,
                writeResultsCache:
                false,
                outputFile: null,
                addInPaths: null,
                loadFromDefaultPath: true); // Loads rules next to StyleCop.dll in the file system.

            var projectDirectory = Path.GetDirectoryName(_projectFile);

            // It's OK if the settings file is null.
            var settingsFile = FindSettingsFile(projectDirectory);

            var project = new CodeProject(0, projectDirectory, new Configuration(null));
            foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                runner.Core.Environment.AddSourceCode(project, file, context: null);
            }

            try
            {
                runner.OutputGenerated += OnOutputGenerated;
                runner.ViolationEncountered += OnViolationEncountered;

                if (settingsFile == null)
                {
                    runner.Core.Analyze(new CodeProject[] { project });
                }
                else
                {
                    runner.Core.Analyze(new CodeProject[] { project }, settingsFile);
                }
            }
            finally
            {
                runner.OutputGenerated -= OnOutputGenerated;
                runner.ViolationEncountered -= OnViolationEncountered;
            }

            return _violations;
        }

        // Performs an ascending directory search starting at the project directory
        private static string FindSettingsFile(string projectDirectory)
        {
            var current = new DirectoryInfo(projectDirectory);
            var root = current.Root;

            do
            {
                var settingsFile = Path.Combine(current.FullName, "Settings.StyleCop");
                if (File.Exists(settingsFile))
                {
                    return settingsFile;
                }
            }
            while ((current = current.Parent) != null);

            return null;
        }

        private void Runner_ViolationEncountered(object sender, ViolationEventArgs e)
        {
            _violations.Add(e.Violation);
        }
    }
}
