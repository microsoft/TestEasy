using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Web.Administration;
using Moq;
using TestEasy.Core.Configuration;
using TestEasy.Core.Helpers;
using TestEasy.TestHelpers;

namespace TestEasy.WebServer.Test
{
    internal class WebServerMockGenerator : MockGenerator
    {
        public Mock<IServerManagerProvider> ServerManagerProvider { get; private set; }
        public Mock<ToolsHelper> ToolsHelper { get; private set; }
        public Mock<BuildHelper> BuildHelper { get; private set; }

        public WebServerMockGenerator()
        {
            ServerManagerProvider = new Mock<IServerManagerProvider>(MockBehavior.Strict);
            ToolsHelper = new Mock<ToolsHelper>(MockBehavior.Strict);
            BuildHelper = new Mock<BuildHelper>(MockBehavior.Strict);
        }

        public WebApplicationInfo MockWebApplicationInfo = new WebApplicationInfo
        {
            PhysicalPath = @"c:\myphysicalpath",
            Name = "MyApp"
        };

        public IEnumerable<DeploymentItem> MockDeploymentList = new List<DeploymentItem>
        {
            new DeploymentItem { Type = DeploymentItemType.File, Path = @"c:\temp\myfile.txt" },
            new DeploymentItem { Type = DeploymentItemType.Directory, Path = @"c:\temp\MyFolder", TargetRelativePath = "rel1" },
            new DeploymentItem { Type = DeploymentItemType.Content, Path = "newfile.txt", Content = @"xxxxx", TargetRelativePath = "rel2" }
        };

        public WebServerMockGenerator MockWebServerDeployWebApp()
        {
            FileSystem.Setup(f => f.DirectoryExists(MockWebApplicationInfo.PhysicalPath)).Returns(false);
            FileSystem.Setup(f => f.DirectoryCreate(MockWebApplicationInfo.PhysicalPath));
            FileSystem.Setup(f => f.FileCopy(@"c:\temp\myfile.txt", @"c:\myphysicalpath\myfile.txt", true));

            var fullPath2 = Path.Combine(MockWebApplicationInfo.PhysicalPath, "rel1");
            FileSystem.Setup(f => f.DirectoryExists(fullPath2)).Returns(false);
            FileSystem.Setup(f => f.DirectoryCreate(fullPath2));
            FileSystem.Setup(f => f.DirectoryCopy(@"c:\temp\MyFolder", fullPath2));

            var fullPath3 = Path.Combine(MockWebApplicationInfo.PhysicalPath, "rel2");
            FileSystem.Setup(f => f.DirectoryExists(fullPath3)).Returns(false);
            FileSystem.Setup(f => f.DirectoryCreate(fullPath3));
            FileSystem.Setup(f => f.FileWrite(Path.Combine(fullPath3, "newfile.txt"), @"xxxxx"));
            return this;
        }

        public string MockWebConfigPath = @"c:\path\web.config";
        public WebServerMockGenerator MockSetCustomHeadersNoWebConfig()
        {
            FileSystem.Setup(f => f.FileExists(MockWebConfigPath)).Returns(false);
            FileSystem.Setup(f => f.FileWrite(MockWebConfigPath, @"<configuration><system.webServer><httpProtocol><customHeaders><clear /><add name=""header1"" value=""value1"" /><add name=""header2"" value=""value2"" /></customHeaders></httpProtocol></system.webServer></configuration>"));

            return this;
        }

        public WebServerMockGenerator MockSetCustomHeadersWithWebConfig()
        {
            FileSystem.Setup(f => f.FileExists(MockWebConfigPath)).Returns(false);
            FileSystem.Setup(f => f.FileReadAllText(MockWebConfigPath)).Returns("");
            FileSystem.Setup(f => f.FileWrite(MockWebConfigPath, @"<configuration><system.webServer><httpProtocol><customHeaders><clear /><add name=""header1"" value=""value1"" /><add name=""header2"" value=""value2"" /></customHeaders></httpProtocol></system.webServer></configuration>"));

            return this;
        }
        public WebServerMockGenerator MockBuildHelperNoProjectName()
        {
            BuildHelper.Setup(f => f.Build(MockUniqueTempPath, MockCreateWebSiteName));

            return this;
        }

        public string MockProjectName = "MyProjectName";
        public WebServerMockGenerator MockBuildHelperWithProjectName()
        {
            BuildHelper.Setup(f => f.Build(MockUniqueTempPath, MockProjectName));

            return this;
        }

        public WebServerMockGenerator MockWebServerIisInConfig()
        {
            TestEasyConfig.Instance.WebServer.Type = "IIS";

            return this;
        }

        public string MockWebServerName = "MockWebServer";
        public string MockWebServerType = "TestEasy.WebServer.Test.MockWebServer";
        public WebServerMockGenerator MockWebServer()
        {
            TestEasyConfig.Instance.WebServer.Type = MockWebServerName;
            TestEasyConfig.Instance.WebServer.Types.Add(new WebServerTypeElement
            {
                Name = MockWebServerName,
                Type = MockWebServerType
            });

            return this;
        }

        public string MockCreateWebSiteName = "MyWebSite";
        public WebServerMockGenerator MockWebServerCreateWithWebSiteProvided()
        {
            TestEasyConfig.Instance.WebServer.StartupTimeout = 100000;

            return this;
        }

        public string MockMachineName = "MyMachine";
        public WebServerMockGenerator MockWebServerCreateWithWebSiteProvidedRemoteTrue()
        {
            TestEasyConfig.Instance.WebServer.StartupTimeout = 100000;
            EnvironmentSystem.Setup(m => m.MachineName).Returns(MockMachineName);

            return this;
        }

        public string MockUniqueTempPath = @"x:\MyUniquePath";
        public WebServerMockGenerator MockToolsHelperGetUniqueTempPath(string path)
        {
            ToolsHelper.Setup(f => f.GetUniqueTempPath(path)).Returns(MockUniqueTempPath);

            return this;
        }

        public string MockCustomAssemblyName = "MyCustomAssembly";
        public WebServerMockGenerator MockWebServerFromCustomAssembly()
        {
            TestEasyConfig.Instance.WebServer.Type = MockWebServerName;
            TestEasyConfig.Instance.WebServer.Types.Add(new WebServerTypeElement
            {
                Name = MockWebServerName,
                Type = MockWebServerType + ", " + MockCustomAssemblyName
            });

            return this;
        }

        public WebServerMockGenerator MockWebServerInstantiateFromCustomAssembly()
        {
            FileSystem.Setup(f => f.GetCallingAssembly()).Returns((Assembly)null);
            FileSystem.Setup(f => f.GetExecutingAssemblyDirectory()).Returns(@"c:\test");
            FileSystem.Setup(f => f.FileExists(@"c:\test\" + MockCustomAssemblyName + ".dll")).Returns(false);
            FileSystem.Setup(f => f.FileExists(@"c:\test\" + MockCustomAssemblyName + ".exe")).Returns(false);
            FileSystem.Setup(f => f.LoadAssemblyFromFile(MockCustomAssemblyName)).Returns((Assembly)null);
            
            return this;
        }


        public WebServerMockGenerator MockWebServerInstantiateFromCurrentAssembly()
        {
            FileSystem.Setup(f => f.GetCallingAssembly()).Returns(Assembly.GetExecutingAssembly());

            return this;
        }

        public WebServerMockGenerator MockWebServerRegisteredButTypeInfoIsEmpty()
        {
            TestEasyConfig.Instance.WebServer.Type = MockWebServerName;
            TestEasyConfig.Instance.WebServer.Types.Add(new WebServerTypeElement
            {
                Name = MockWebServerName,
                Type = ""
            });

            return this;            
        }

        private string _applicationHostTemplate;
        public string AplicationHostTemplate
        {
            get
            {
                if (string.IsNullOrEmpty(_applicationHostTemplate) || File.Exists(_applicationHostTemplate) == false)
                {
                    var assembly = Assembly.GetExecutingAssembly();

                    using (var stream = assembly.GetManifestResourceStream("TestEasy.WebServer.Test.applicationhost_template.config"))
                    {
                        if (stream == null) return null;

                        using (var reader = new StreamReader(stream))
                        {
                            var currentDir = Path.GetDirectoryName(assembly.Location) ?? "";
                            _applicationHostTemplate = Path.Combine(currentDir, "applicationhost_template.config");
                            File.WriteAllText(_applicationHostTemplate, reader.ReadToEnd());
                        }
                    }
                }

                return _applicationHostTemplate;
            }
        }

        private ServerManager _serverManager;
        public ServerManager ServerManager
        {
            get { return _serverManager ?? (_serverManager = new ServerManager(AplicationHostTemplate)); }
        }

        public WebServerMockGenerator MockApplicationDeploySourceDoesNotExist()
        {
            FileSystem.Setup(f => f.DirectoryExists(@"c:\Temp\MySite")).Returns(false);

            return this;
        }

        public string MockApplicationName { get; set; }
        public WebServerMockGenerator MockApplication()
        {
            MockApplicationName = Randomize("/test");
            ServerManager.Sites[0].Applications.Add(MockApplicationName, @"c:\temp\target");

            return this;
        }

        public WebServerMockGenerator MockApplicationVirtualDirectoryDoesNotExist()
        {
            var app = ServerManager.Sites[0].Applications[MockApplicationName];
            app.VirtualDirectories.Clear();

            FileSystem.Setup(f => f.DirectoryExists(@"c:\Temp\MySite")).Returns(true);

            return this;
        }

        public WebServerMockGenerator MockApplicationPhysicalDirectoryExist()
        {
            FileSystem.Setup(f => f.DirectoryExists(@"c:\Temp\MySite")).Returns(true);

            return this;
        }

        public WebServerMockGenerator MockApplicationDeployVirtualDirectoryPathNotExistShouldCreate()
        {
            FileSystem.Setup(f => f.DirectoryExists(@"c:\Temp\MySite")).Returns(true);
            FileSystem.Setup(f => f.DirectoryExists(@"c:\temp\target")).Returns(false);
            FileSystem.Setup(f => f.DirectoryCreate(@"c:\temp\target"));
            FileSystem.Setup(f => f.DirectoryCopy(@"c:\Temp\MySite", @"c:\temp\target"));
            FileSystem.Setup(f => f.DirectorySetAttribute(@"c:\temp\target", FileAttributes.Normal));

            return this;
        }

        public WebServerMockGenerator MockApplicationDeployVirtualDirectoryPathExistShouldJustCopy()
        {
            FileSystem.Setup(f => f.DirectoryExists(@"c:\Temp\MySite")).Returns(true);
            FileSystem.Setup(f => f.DirectoryExists(@"c:\temp\target")).Returns(true);
            FileSystem.Setup(f => f.DirectoryCopy(@"c:\Temp\MySite", @"c:\temp\target"));
            FileSystem.Setup(f => f.DirectorySetAttribute(@"c:\temp\target", FileAttributes.Normal));

            return this;
        }

        public WebServerMockGenerator MockApplicationDeployListOfFilesIfListIsEmptyShouldThrow()
        {
            FileSystem.Setup(f => f.DirectoryExists(@"c:\Temp\MySite")).Returns(true);
            FileSystem.Setup(f => f.DirectoryExists(@"c:\temp\target")).Returns(true);
            FileSystem.Setup(f => f.DirectoryCopy(@"c:\Temp\MySite", @"c:\temp\target"));

            return this;
        }

        public WebServerMockGenerator MockApplicationDeployListOfFilesShouldCopyExistingFilesToRelativeSubFolder()
        {
            FileSystem.Setup(f => f.DirectoryExists(@"c:\temp\target")).Returns(false);
            FileSystem.Setup(f => f.DirectoryCreate(@"c:\temp\target"));
            FileSystem.Setup(f => f.DirectoryExists(@"c:\temp\target\relative")).Returns(false);
            FileSystem.Setup(f => f.DirectoryCreate(@"c:\temp\target\relative"));

            FileSystem.Setup(f => f.FileExists(@"c:\Temp\MySite\file1.html")).Returns(true);
            FileSystem.Setup(f => f.FileExists(@"c:\Temp\MySite\file2.html")).Returns(true);
            FileSystem.Setup(f => f.FileExists(@"c:\Temp\MySite\file3.html")).Returns(false);
            FileSystem.Setup(f => f.FileCopy(@"c:\Temp\MySite\file1.html", @"c:\temp\target\relative\file1.html", true));
            FileSystem.Setup(f => f.FileCopy(@"c:\Temp\MySite\file2.html", @"c:\temp\target\relative\file2.html", true));

            return this;
        }

        public string MockRelativePath = "relative";
        public string MockSampleFileContent = "file content";
        public WebServerMockGenerator MockApplicationDeployFileContentShouldCreateFile()
        {
            FileSystem.Setup(f => f.FileWrite(Path.Combine(@"c:\temp\target", MockRelativePath), MockSampleFileContent));

            return this;
        }

        public string MockUniqueApplicationName
        {
            get
            {
                return Randomize("test");  
            } 
        }

        public string MockWwwRootPath = @"x:\wwwroot";
        public string MockInetInfoPath = @"x:somepath\ineinfo.exe";
        public string MockApplicationHostConfig = "myapphost.config";
        public WebServerMockGenerator MockWebServerIisCtor_InetInfoFileExists()
        {
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\inetpub\wwwroot")).Returns(MockWwwRootPath);
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%windir%\Sysnative\inetsrv\inetinfo.exe")).Returns(MockInetInfoPath);
            FileSystem.Setup(f => f.FileExists(MockInetInfoPath)).Returns(true);
            FileSystem.Setup(f => f.FileGetVersion(MockInetInfoPath)).Returns(new Version("11.0"));
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\System32\inetsrv\config\applicationHost.config")).Returns(MockApplicationHostConfig);
            ServerManagerProvider.Setup(f => f.GetServerManager("")).Returns(ServerManager);

            return this;
        }

        public WebServerMockGenerator MockWebServerIisCtor_InetInfoFileDoesNotExist_OSVersion(string osVersion)
        {
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\inetpub\wwwroot")).Returns(MockWwwRootPath);
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%windir%\Sysnative\inetsrv\inetinfo.exe")).Returns(MockInetInfoPath);
            FileSystem.Setup(f => f.FileExists(MockInetInfoPath)).Returns(false);
            EnvironmentSystem.Setup(f => f.OSVersion).Returns(new Version(osVersion));
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\System32\inetsrv\config\applicationHost.config")).Returns(MockApplicationHostConfig);
            ServerManagerProvider.Setup(f => f.GetServerManager("")).Returns(ServerManager);

            return this;
        }

        public WebServerMockGenerator MockWebServerIisCtor_InetInfoFileDoesNotExist_OSVersionUnknown()
        {
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\inetpub\wwwroot")).Returns(MockWwwRootPath);
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%windir%\Sysnative\inetsrv\inetinfo.exe")).Returns(MockInetInfoPath);
            EnvironmentSystem.Setup(f => f.MachineName).Returns(MockMachineName);
            FileSystem.Setup(f => f.FileExists(MockInetInfoPath)).Returns(false);
            EnvironmentSystem.Setup(f => f.OSVersion).Returns(new Version("160.0"));
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\System32\inetsrv\config\applicationHost.config")).Returns(MockApplicationHostConfig);
            ServerManagerProvider.Setup(f => f.GetServerManager("")).Returns(ServerManager);

            return this;
        }

        public WebServerMockGenerator MockWebServerCreate_RemoteTrue()
        {
            TestEasyConfig.Instance.Client.Remote = true;

            return this;
        }

        public WebServerMockGenerator MockWebServerCreate_RemoteFalse()
        {
            TestEasyConfig.Instance.Client.Remote = false;

            return this;
        }

        public string MockIisExpressPath = @"x:\somepath\iisexpress.exe";
        public string MockIisExpressPath86 = @"x:\somepath86\iisexpress.exe";
        public WebServerMockGenerator MockWebServerIisExpressCtor_GetNextAvailablePort()
        {
            EnvironmentSystem.Setup(f => f.GetNextAvailablePort(0)).Returns(1000);

            return this;
        }

        public string MockCurrentExecutableDir = @"c:\currentdir\current.dll";
        public WebServerMockGenerator MockIisExpressRootPathInCurrentDir()
        {
            FileSystem.Setup(f => f.GetExecutingAssemblyDirectory()).Returns(MockCurrentExecutableDir);

            return this;
        }

        public WebServerMockGenerator MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles()
        {
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%PROGRAMFILES%\IIS Express\iisexpress.exe")).Returns(MockIisExpressPath);
            FileSystem.Setup(f => f.FileExists(MockIisExpressPath)).Returns(true);
            FileSystem.Setup(f => f.FileGetVersion(MockIisExpressPath)).Returns(new Version("11.0"));

            return this;
        }

        public WebServerMockGenerator MockApplicationDeployVirtualDirectoryPathExistIfRelativePathProvidedShouldCopyToRelative()
        {
            FileSystem.Setup(f => f.DirectoryExists(@"c:\Temp\MySite")).Returns(true);
            FileSystem.Setup(f => f.DirectoryExists(@"c:\temp\target")).Returns(true);
            FileSystem.Setup(f => f.DirectoryExists(@"c:\temp\target\..\otherfolder")).Returns(false);
            FileSystem.Setup(f => f.DirectoryCreate(@"c:\temp\target\..\otherfolder"));
            FileSystem.Setup(f => f.DirectoryCopy(@"c:\Temp\MySite", @"c:\temp\target\..\otherfolder"));

            return this;
        }

        public string MockApplicationHostConfigPath;
        public WebServerMockGenerator MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(string path)
        {
            var template = Path.Combine(Path.GetDirectoryName(path) ?? "", @"config\templates\PersonalWebServer\applicationhost.config");
            MockApplicationHostConfigPath = Path.Combine(Path.GetDirectoryName(MockCurrentExecutableDir), "applicationhost.config");
            FileSystem.Setup(f => f.FileExists(template)).Returns(true);
            FileSystem.Setup(f => f.FileCopy(template, MockApplicationHostConfigPath, true));

            return this;
        }

        public WebServerMockGenerator MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found_UniqueAppPath(string path)
        {
            var template = Path.Combine(Path.GetDirectoryName(path) ?? "", @"config\templates\PersonalWebServer\applicationhost.config");
            MockApplicationHostConfigPath = Path.Combine(MockUniqueTempPath, "applicationhost.config");
            FileSystem.Setup(f => f.FileExists(template)).Returns(true);
            FileSystem.Setup(f => f.FileCopy(template, MockApplicationHostConfigPath, true));

            return this;
        }

        public WebServerMockGenerator MockWebServerIisExpress_DeleteApp(string path)
        {
            FileSystem.Setup(f => f.DirectoryDelete(path, true));

            return this;
        }

        public WebServerMockGenerator MockWebServerIisExpressStart(string path)
        {
            ProcessRunner.Setup(f => f.Start(It.Is<Process>(
                p => p.StartInfo.FileName.Equals(path)
                     && p.StartInfo.Arguments.Equals(MockIssExpressProcessArguments)))).Returns(true);

            ProcessRunner.Setup(f => f.WaitForExit(It.Is<Process>(
                p => p.StartInfo.FileName.Equals(path)
                     && p.StartInfo.Arguments.Equals(MockIssExpressProcessArguments)), 5000)).Returns(false);

            return this;
        }

        public string MockIssExpressProcessArguments
        {
            get
            {
                return string.Format(WebServerIisExpress.CommandLineArgsFormat, MockApplicationHostConfigPath);
            }
        }
       
        public WebServerMockGenerator MockWebServerIisExpressStop(string path)
        {
            ProcessRunner.Setup(f => f.Stop(It.Is<Process>(
                p => p.StartInfo.FileName.Equals(path)
                     && p.StartInfo.Arguments.Equals(MockIssExpressProcessArguments))));

            return this;
        }

        #region IisExpressConfig

        public WebServerMockGenerator MockIisExpressConfig_ApplicationConfigNotFound(string path)
        {
            FileSystem.Setup(f => f.FileExists(path)).Returns(false);
            return this;
        }

        public WebServerMockGenerator MockIisExpressConfig_ApplicationConfigFound(string path)
        {
            FileSystem.Setup(f => f.FileExists(path)).Returns(true);
            return this;
        }

        public WebServerMockGenerator MockIisExpressConfig_DocumentNotLoaded(string path)
        {
            FileSystem.Setup(f => f.LoadXDocumentFromFile(path)).Returns((XDocument)null);
            return this;
        }

        public XDocument MockXDocument = new XDocument();
        public XDocument MockXDocumentInitialized = null;
        public WebServerMockGenerator MockIisExpressConfig_DocumentLoaded(string path)
        {
            FileSystem.Setup(f => f.LoadXDocumentFromFile(path)).Returns(MockXDocument);
            return this;
        }

        public WebServerMockGenerator MockIisExpressConfig_RealDocumentInitialized(string path)
        {
            MockXDocumentInitialized = XDocument.Load(AplicationHostTemplate);
            FileSystem.Setup(f => f.LoadXDocumentFromFile(path)).Returns(MockXDocumentInitialized);
            return this;
        }

        public WebServerMockGenerator MockIisExpressConfig_StoreSchema(string path)
        {
            FileSystem.Setup(f => f.StoreXDocumentToFile(It.IsAny<XDocument>(), path));
            return this;
        }        
       
        #endregion
    }
}
