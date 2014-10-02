using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace TestEasy.Core.Configuration
{
    public class TestEasyConfig
    {
        internal const string DefaultLocalToolsPath = @"%SystemDrive%\fxttools";
        internal const string TestEasySupportKey = "TestEasySupportPath";
        internal const string TestEasySupportEnvVar = "%TestEasySupportPath%";

        private static TestEasyConfig _instance;
        public static TestEasyConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TestEasyConfig();
                }

                return _instance;
            }

            internal set { _instance = value; }
        }

        public const string ConfigDefault = "default.config";
        public const string ConfigTestsuite = "testsuite.config";
        public const string ConfigContext = "context.config";

        private readonly System.Configuration.Configuration _configuration;
        private WebServerSection _webServerSection;
        private ClientSection _clientSection;
        private AzureSection _azureSection;
        private ToolsSection _toolsSection;

        public TestEasyConfig()
            : this("")
        {
            
        }

        public TestEasyConfig(string workingDir)
        {
            if (string.IsNullOrEmpty(workingDir))
            {
                workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            }

            var fileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = Path.Combine(workingDir, ConfigTestsuite),
                    LocalUserConfigFilename = Path.Combine(workingDir, ConfigContext)
                };

            // try to find default.config
            var toolsPath = workingDir; // first try current directory
            if (!File.Exists(Path.Combine(toolsPath, ConfigDefault)))
            {
                // then try some custom path
                toolsPath = Environment.ExpandEnvironmentVariables(TestEasySupportEnvVar);
                if (string.IsNullOrEmpty(toolsPath) || toolsPath.Equals(TestEasySupportEnvVar))
                {
                    // finally use hardcoded - default location that we use internally
                    toolsPath = ConfigurationManager.AppSettings[TestEasySupportKey];
                }
            }

            var defaultConfigPath = Path.Combine(toolsPath ?? "", ConfigDefault);
            if (File.Exists(defaultConfigPath))
            {
                fileMap.MachineConfigFilename = defaultConfigPath;
            }
            else
            {
                // if default.config is not found, then sections in testsuite.config are undefined,
                // thus we need to define them here to unblock a user (since all settings also can
                // be specified in testsuite configs if users would like that)
                _configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                // add sections to config (users may forget to add them)          
                AddSafeSection("webServer", new WebServerSection());
                AddSafeSection("client", new ClientSection());
                AddSafeSection("azure", new AzureSection());
                AddSafeSection("tools", new ToolsSection());

                _configuration.Save();
            }

            // reopen config and reparse it
            _configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);          

            SetEnvironmentVariables();            
        }

        internal TestEasyConfig(object dummy)
        {
            var workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            var fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = Path.Combine(workingDir, ConfigTestsuite),
                LocalUserConfigFilename = Path.Combine(workingDir, ConfigContext)
            };

            // try to find default.config
            var toolsPath = workingDir; // first try current directory
            var defaultConfigPath = Path.Combine(toolsPath ?? "", ConfigDefault);
            if (File.Exists(defaultConfigPath))
            {
                fileMap.MachineConfigFilename = defaultConfigPath;
            }
            else
            {
                // if default.config is not found, then sections in testsuite.config are undefined,
                // thus we need to define them here to unblock a user (since all settings also can
                // be specified in testsuite configs if users would like that)
                _configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                // add sections to config (users may forget to add them)          
                AddSafeSection("webServer", new WebServerSection());
                AddSafeSection("client", new ClientSection());
                AddSafeSection("azure", new AzureSection());
                AddSafeSection("tools", new ToolsSection());

                _configuration.Save();
            }

            // reopen config and reparse it
            _configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            SetEnvironmentVariables();            
        }

        private void AddSafeSection(string name, ConfigurationSection section)
        {
            if (_configuration.Sections[name] != null)
            {
                _configuration.Sections.Remove(name);
            }

            _configuration.Sections.Add(name, section);
        }

        private void SetEnvironmentVariables()
        {
            var localToolsPath = Environment.ExpandEnvironmentVariables(DefaultLocalToolsPath).ToLowerInvariant();

            var targets = new List<EnvironmentVariableTarget>
                {
                    EnvironmentVariableTarget.Process,
                    EnvironmentVariableTarget.User,
                    EnvironmentVariableTarget.Machine
                };

            foreach (var target in targets)
            {
                var path = (Environment.GetEnvironmentVariable("PATH", target) ?? "").ToLowerInvariant();

                if (!string.IsNullOrEmpty(path) && path.Contains(localToolsPath)) continue;

                Environment.SetEnvironmentVariable("PATH", path + ";" + localToolsPath, target);
            }
        }

        public WebServerSection WebServer
        {
            get
            {
                if (_webServerSection == null)
                {
                    _webServerSection = (WebServerSection)_configuration.GetSection("webServer") ?? new WebServerSection();
                }

                return _webServerSection;
            }
        }

        public ClientSection Client
        {
            get
            {
                if (_clientSection == null)
                {
                    _clientSection = (ClientSection)_configuration.GetSection("client") ?? new ClientSection();
                }

                return _clientSection;
            }
        }

        public AzureSection Azure
        {
            get
            {
                if (_azureSection == null)
                {
                    _azureSection = (AzureSection)_configuration.GetSection("azure") ?? new AzureSection();
                }

                return _azureSection;
            }
        }

        public ToolsSection Tools
        {
            get
            {
                if (_toolsSection == null)
                {
                    _toolsSection = (ToolsSection)_configuration.GetSection("tools") ?? new ToolsSection();
                }

                return _toolsSection;
            }
        }
    }    
}
