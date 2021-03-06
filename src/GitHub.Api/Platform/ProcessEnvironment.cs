using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace GitHub.Unity
{
    class ProcessEnvironment : IProcessEnvironment
    {
        protected IEnvironment Environment { get; private set; }
        protected ILogging Logger { get; private set; }

        public ProcessEnvironment(IEnvironment environment)
        {
            Logger = Logging.GetLogger(GetType());
            Environment = environment;
        }

        public NPath FindRoot(NPath path)
        {
            if (path == null)
            {
                return null;
            }
            
            if (path.FileExists())
                path = path.Parent;

            if (path.Combine(".git").DirectoryExists())
            {
                return path;
            }

            if (path.IsEmpty)
                return null;

            return FindRoot(path.Parent);
        }

        public void Configure(ProcessStartInfo psi, NPath workingDirectory)
        {
            psi.WorkingDirectory = workingDirectory;
            psi.EnvironmentVariables["HOME"] = NPath.HomeDirectory;
            psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = NPath.SystemTemp;

            // if we don't know where git is, then there's nothing else to configure
            if (Environment.GitInstallPath == null)
                return;


            Guard.ArgumentNotNull(psi, "psi");

            // We need to essentially fake up what git-cmd.bat does

            var gitPathRoot = Environment.GitInstallPath;
            var gitLfsPath = Environment.GitInstallPath;

            // Paths to developer tools such as msbuild.exe
            //var developerPaths = StringExtensions.JoinForAppending(";", developerEnvironment.GetPaths());
            var developerPaths = "";

            //TODO: Remove with Git LFS Locking becomes standard
            psi.EnvironmentVariables["GITLFSLOCKSENABLED"] = "1";

            string path;
            var baseExecPath = gitPathRoot;
            var binPath = baseExecPath;
            if (Environment.IsWindows)
            {
                if (baseExecPath.DirectoryExists("mingw32"))
                    baseExecPath = baseExecPath.Combine("mingw32");
                else
                    baseExecPath = baseExecPath.Combine("mingw64");
                binPath = baseExecPath.Combine("bin");
            }
            var execPath = baseExecPath.Combine("libexec", "git-core");

            if (Environment.IsWindows)
            {
                var userPath = @"C:\windows\system32;C:\windows";
                path = String.Format(CultureInfo.InvariantCulture, @"{0}\cmd;{0}\usr\bin;{1};{2};{0}\usr\share\git-tfs;{3};{4}{5}",
                    gitPathRoot, execPath, binPath,
                    gitLfsPath, userPath, developerPaths);
            }
            else
            {
                var userPath = Environment.Path;
                path = String.Format(CultureInfo.InvariantCulture, @"{0}:{1}:{2}:{3}{4}",
                    binPath, execPath, gitLfsPath, userPath, developerPaths);
            }
            psi.EnvironmentVariables["GIT_EXEC_PATH"] = execPath.ToString();

            psi.EnvironmentVariables["PATH"] = path;

            psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
            psi.EnvironmentVariables["TERM"] = "msys";

            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!String.IsNullOrEmpty(httpProxy))
                psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;

            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!String.IsNullOrEmpty(httpsProxy))
                psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;
        }
    }
}