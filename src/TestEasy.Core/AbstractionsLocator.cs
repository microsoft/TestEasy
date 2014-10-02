using TestEasy.Core.Abstractions;

namespace TestEasy.Core
{
    /// <summary>
    ///     Centralized container for dependencies abstracted for unit testing (use Ninject later)
    /// </summary>
    public class AbstractionsLocator
    {
        private static AbstractionsLocator _instance;
        public static AbstractionsLocator Instance
        {
            get { return _instance ?? (_instance = new AbstractionsLocator()); }
            internal set { _instance = value; }           
        }

        private IFileSystem _fileSystem;
        public IFileSystem FileSystem
        {
            get { return _fileSystem ?? (_fileSystem = new FileSystem()); }
            internal set { _fileSystem = value; }
        }

        private IRegistrySystem _registrySystem;
        public IRegistrySystem RegistrySystem
        {
            get { return _registrySystem ?? (_registrySystem = new RegistrySystem()); }
            internal set { _registrySystem = value; }
        }

        private IEnvironmentSystem _environmentSystem;
        public IEnvironmentSystem EnvironmentSystem
        {
            get { return _environmentSystem ?? (_environmentSystem = new EnvironmentSystem()); }
            internal set { _environmentSystem = value; }
        }

        private IWebRequestor _webRequestor;
        public IWebRequestor WebRequestor
        {
            get { return _webRequestor ?? (_webRequestor = new WebRequestor()); }
            internal set { _webRequestor = value; }
        }

        private IProcessRunner _processRunner;
        public IProcessRunner ProcessRunner
        {
            get { return _processRunner ?? (_processRunner = new ProcessRunner()); }
            internal set { _processRunner = value; }
        }
    }
}
