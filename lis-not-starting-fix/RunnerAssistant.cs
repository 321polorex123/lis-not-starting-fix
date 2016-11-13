using OpenSupportEngine.Helpers.FileSystem;
using OpenSupportEngine.Helpers.Resources;
using OpenSupportEngine.Logging.LoggingProvider;
using OpenSupportEngine.TaskRunner.Runners;
using OpenSupportEngine.TaskRunner.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace lis_not_starting_fix
{
    class NextInstallationReachedEventArgs : EventArgs
    {
        public string UiString { get; set; }
        public int TaskCount { get; set; }
    }

    class InstallationFinishedEventArgs : EventArgs
    {
        public bool Successful { get; set; }
    }

    class RunnerAssistant
    {
        public static readonly Dictionary<string, string> UiStrings =
            new Dictionary<string, string>()
            {
                {"lis_not_starting_fix.Resources.Data.vcredist2008sp1x86.vcredist_x86.exe",
                    "Microsoft Visual C++ 2008 SP1 Redistributable Package (x86)" },
                {"lis_not_starting_fix.Resources.Data.vcredist2008sp1x64.vcredist_x64.exe",
                    "Microsoft Visual C++ 2008 SP1 Redistributable Package (x64)" },
                {"lis_not_starting_fix.Resources.Data.vcredist2010sp1x86.vcredist_x86.exe",
                    "Microsoft Visual C++ 2010 SP1 Redistributable Package (x86)" },
                {"lis_not_starting_fix.Resources.Data.vcredist2010sp1x64.vcredist_x64.exe",
                    "Microsoft Visual C++ 2010 SP1 Redistributable Package (x64)" },
                {"lis_not_starting_fix.Resources.Data.vcredist2012up4x86.vcredist_x86.exe",
                    "Visual C++ Redistributable for Visual Studio 2012 Update 4 (x86)" },
                {"lis_not_starting_fix.Resources.Data.vcredist2012up4x64.vcredist_x64.exe",
                    "Visual C++ Redistributable for Visual Studio 2012 Update 4 (x64)" }
            };

        public int TaskCount
        {
            get { return actualUiStrings.Count; }
        }
        public bool IsRunning
        {
            get { return runner.Running; }
        }

        private TemporaryFolder tmpFolder = new TemporaryFolder();
        private AsynchronousRunner runner = new AsynchronousRunner();
        private Dictionary<uint, string> actualUiStrings = new Dictionary<uint, string>();
        private int doneTaskCounter = 1;

        public event EventHandler<NextInstallationReachedEventArgs> NextInstallationReached;
        public event EventHandler<InstallationFinishedEventArgs> InstallationFinished;

        public RunnerAssistant()
        {
            runner.Logger = new DummyLoggingProvider();
            InitTasks();

            runner.TaskFinished += (s, e) =>
            {
                if (actualUiStrings.Keys.ToList().Contains(e.Task.ID))
                {
                    var eventArgs = new NextInstallationReachedEventArgs()
                    {
                        UiString = actualUiStrings[e.Task.ID],
                        TaskCount = doneTaskCounter++
                    };
                    Action a = () =>
                    {
                        NextInstallationReached?.Invoke(this, eventArgs);
                    };
                    Application.Current.Dispatcher.BeginInvoke(a);
                }
            };
            runner.RunnerFinished += (s, e) =>
            {
                var eventArgs = new InstallationFinishedEventArgs()
                {
                    Successful = e.WasSuccessful
                };
                Action a = () =>
                {
                    InstallationFinished?.Invoke(this, eventArgs);
                };
                Application.Current.Dispatcher.BeginInvoke(a);
            };
        }

        public void RemoveTempFolder()
        {
            tmpFolder.Dispose();
        }

        private void InitTasks()
        {
            var x86Files = new List<string>()
            {
                // Microsoft Visual C++ 2008 SP1 Redistributable Package (x86)
                "lis_not_starting_fix.Resources.Data.vcredist2008sp1x86.vcredist_x86.exe",
                // Microsoft Visual C++ 2010 SP1 Redistributable Package (x86)
                "lis_not_starting_fix.Resources.Data.vcredist2010sp1x86.vcredist_x86.exe",
                // Visual C++ Redistributable for Visual Studio 2012 Update 4 (x86)
                "lis_not_starting_fix.Resources.Data.vcredist2012up4x86.vcredist_x86.exe"
            };
            var x64Files = new List<string>()
            {
                // Microsoft Visual C++ 2008 SP1 Redistributable Package (x64)
                "lis_not_starting_fix.Resources.Data.vcredist2008sp1x64.vcredist_x64.exe",
                // Microsoft Visual C++ 2010 SP1 Redistributable Package (x64)
                "lis_not_starting_fix.Resources.Data.vcredist2010sp1x64.vcredist_x64.exe",
                // Visual C++ Redistributable for Visual Studio 2012 Update 4 (x64)
                "lis_not_starting_fix.Resources.Data.vcredist2012up4x64.vcredist_x64.exe"
            };
            var installArgsList = new List<string>()
            {
                // Microsoft Visual C++ 2008 SP1 Redistributable Package
                "/q",
                // Microsoft Visual C++ 2010 SP1 Redistributable Package
                "/q",
                // Visual C++ Redistributable for Visual Studio 2012 Update 4
                "/quiet"
            };
            var uninstallArgsList = new List<string>()
            {
                // Microsoft Visual C++ 2008 SP1 Redistributable Package
                "/qu",
                // Microsoft Visual C++ 2010 SP1 Redistributable Package
                "/uninstall /q",
                // Visual C++ Redistributable for Visual Studio 2012 Update 4
                "/uninstall /quiet"
            };


            var thisAssembly = Assembly.GetAssembly(typeof(RunnerAssistant));
            var targetPath = Path.Combine(tmpFolder.DirectoryPath, "vcredist.exe");

            for (var i = 0; i < x86Files.Count; i++)
            {
                Action<string> run = s =>
                {
                    var descriptor = new ResourceDescriptor()
                    {
                        ResourceAssembly = thisAssembly,
                        FullResourcePath = s
                    };
                    var id = runner.AddTask(
                        new ExtractResourceTask(
                            descriptor,
                            targetPath
                            )
                        );

                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = targetPath,
                        Arguments = uninstallArgsList[i]
                    };
                    runner.AddTask(
                        new RunFileTask(
                            startInfo
                            )
                        );

                    startInfo = new ProcessStartInfo()
                    {
                        FileName = targetPath,
                        Arguments = installArgsList[i]
                    };
                    runner.AddTask(
                        new RunFileTask(
                            startInfo
                            )
                        );

                    runner.AddTask(
                        new SleepTask(
                            500
                            )
                        );

                    actualUiStrings.Add(id, UiStrings[s]);
                };

                run(x86Files[i]);

                if (Environment.Is64BitOperatingSystem)
                    run(x64Files[i]);
            }
        }

        public void Start()
        {
            runner.RunAsynchronously(new object());
        }
    }
}
