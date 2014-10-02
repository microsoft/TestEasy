using TestEasy.Core.Abstractions;

namespace TestEasy.WebServer
{
    /// <summary>
    ///     Contains dependencies that were mocked for unit tests
    /// </summary>
    public class Dependencies
    {
        private static Dependencies _instance;
        public static Dependencies Instance
        {
            get { return _instance ?? (_instance = new Dependencies()); }
            set
            {
                _instance = value;
            }
        }

        private IFileSystem _fileSystem;
        public IFileSystem FileSystem
        {
            get { return _fileSystem ?? (_fileSystem = new FileSystem()); }
            set
            {
                _fileSystem = value;
            }
        }

        private IEnvironmentSystem _environmentSystem;
        public IEnvironmentSystem EnvironmentSystem
        {
            get { return _environmentSystem ?? (_environmentSystem = new EnvironmentSystem()); }
            set
            {
                _environmentSystem = value;
            }
        }

        private IProcessRunner _processRunner;
        public IProcessRunner ProcessRunner
        {
            get { return _processRunner ?? (_processRunner = new ProcessRunner()); }
            set
            {
                _processRunner = value;
            }
        }

        private IServerManagerProvider _serverManagerProvider;
        public IServerManagerProvider ServerManagerProvider
        {
            get { return _serverManagerProvider ?? (_serverManagerProvider = new ServerManagerProvider()); }
            set
            {
                _serverManagerProvider = value;
            }
        }
    }
}
