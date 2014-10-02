using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using Microsoft.Web.Deployment;
using Microsoft.Win32;
using TestEasy.Core;

namespace TestEasy.Azure.Helpers
{
    [Flags]
    internal enum WebDeploySyncDirection
    {
        SourceIsLocal = 0,
        SourceIsRemote = 1,
        DestinationIsLocal = 0,
        DestinationIsRemote = 2,
    }

    public enum WebDeployDatabaseType
    {
        FullSql,
        MySql,
        SqlCe
    }

    /// <summary>
    ///     WebDeploy helper APIs
    /// </summary>
    internal static class WebDeployHelper
    {
        private static bool _initialized;

        private static void InitializeWebDeployment()
        {
            if (!_initialized)
            {
                // workaround - there may by a runtime exception if those reg keys are present 
                AbstractionsLocator.Instance.RegistrySystem.RemoveRegistryKey(Registry.LocalMachine,
                                                @"Software\Microsoft\IIS Extensions\msdeploy\3\extensibility");
                AbstractionsLocator.Instance.RegistrySystem.RemoveRegistryKey(Registry.LocalMachine,
                                                @"Software\Wow6432Node\Microsoft\IIS Extensions\msdeploy\3\extensibility");

                _initialized = true;
            }
        }

        private static void PrepareDatabaseDeployment(WebDeployDatabaseType dbType, out DeploymentWellKnownProvider provider,
                                               out DeploymentBaseOptions options, bool includeData =  false, bool dropDestinationDatabase = false)
        {
            provider = DeploymentWellKnownProvider.DBDacFx;
            options = new DeploymentBaseOptions();
            switch (dbType)
            {
                case WebDeployDatabaseType.SqlCe:
                case WebDeployDatabaseType.FullSql:
                    provider = DeploymentWellKnownProvider.DBDacFx;
                    break;
                case WebDeployDatabaseType.MySql:
                    provider = DeploymentWellKnownProvider.DBMySql;
                    break;
            }

            options.AddDefaultProviderSetting(provider.ToString(), "dropDestinationDatabase", dropDestinationDatabase);
            options.AddDefaultProviderSetting(provider.ToString(), "includeData", includeData);
        }

        /// <summary>
        ///     Deploy database from different sources to different destiations using WebDeploy
        /// </summary>
        /// <param name="sourceDbType"></param>
        /// <param name="sourceConnectionString"></param>
        /// <param name="destinationDbType"></param>
        /// <param name="destinationConnectionString"></param>
        /// <param name="includeData"></param>
        /// <param name="dropDestinationDatabase"></param>
        public static void DeployDatabase(
            WebDeployDatabaseType sourceDbType,
            string sourceConnectionString,
            WebDeployDatabaseType destinationDbType,
            string destinationConnectionString,
            bool includeData = false,
            bool dropDestinationDatabase = false)
        {
            InitializeWebDeployment();

            DeploymentWellKnownProvider srcProvider;
            DeploymentBaseOptions srcBaseOptions;
            PrepareDatabaseDeployment(sourceDbType, out srcProvider, out srcBaseOptions, includeData, dropDestinationDatabase);

            DeploymentWellKnownProvider destProvider;
            DeploymentBaseOptions destBaseOptions;
            PrepareDatabaseDeployment(destinationDbType, out destProvider, out destBaseOptions, includeData, dropDestinationDatabase);

            var destSyncOptions = new DeploymentSyncOptions();
            
            ExecuteDeploy(
                sourceConnectionString,
                destinationConnectionString,
                srcProvider,
                srcBaseOptions,
                destProvider,
                destBaseOptions,
                destSyncOptions);
        }

        /// <summary>
        ///     Deploy website from different sources to different destiations using WebDeploy
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="destinationAddress"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="deleteExisting"></param>
        /// <param name="paramResolverFunc"></param>
        public static void DeployWebSite(
            string sourcePath,
            string destinationPath,
            string destinationAddress,
            string user,
            string password,
            bool deleteExisting = true,
            Func<string, string> paramResolverFunc = null)
        {
            InitializeWebDeployment();

            var skipDirectives = new List<DeploymentSkipDirective>
                {
                    new DeploymentSkipDirective("skipDbFullSql", @"objectName=dbFullSql", true),
                    new DeploymentSkipDirective("skipDbMySql", @"objectName=dbMySql", true)
                };

            // define a source deployment provider and its properties
            const DeploymentWellKnownProvider srcProvider = DeploymentWellKnownProvider.ContentPath;
            var srcBaseOptions = new DeploymentBaseOptions();

            // define a destination deployment provider and its properties
            const DeploymentWellKnownProvider destProvider = DeploymentWellKnownProvider.ContentPath;
            var destBaseOptions = new DeploymentBaseOptions();

            // define a synchronization options and set if we shoudl delete existing files
            var destSyncOptions = new DeploymentSyncOptions { DoNotDelete = !deleteExisting };

            Deploy(
                sourcePath, 
                destinationPath, 
                destinationAddress, 
                user, 
                password, 
                srcProvider, 
                srcBaseOptions, 
                destProvider, 
                destBaseOptions, 
                destSyncOptions, 
                paramResolverFunc, 
                skipDirectives.AsEnumerable());
        }

        /// <summary>
        ///     Deploy file from different sources to different destiations using WebDeploy
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="destinationAddress"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="deleteExisting"></param>
        /// <param name="paramResolverFunc"></param>
        public static void DeployFile(
            string sourcePath,
            string destinationPath,
            string destinationAddress, 
            string user,
            string password,
            bool deleteExisting = true,
            Func<string, string> paramResolverFunc = null)
        {
            InitializeWebDeployment();

            // define a source deployment provider and its properties
            const DeploymentWellKnownProvider srcProvider = DeploymentWellKnownProvider.ContentPath;
            var srcBaseOptions = new DeploymentBaseOptions();

            // define a destination deployment provider and its properties
            const DeploymentWellKnownProvider destProvider = DeploymentWellKnownProvider.ContentPath;
            var destBaseOptions = new DeploymentBaseOptions();

            // define a synchronization options and set if we shoudl delete existing files
            var destSyncOptions = new DeploymentSyncOptions { DoNotDelete = !deleteExisting };

            Deploy(
                sourcePath,
                destinationPath,
                destinationAddress,
                user,
                password,
                srcProvider,
                srcBaseOptions,
                destProvider,
                destBaseOptions,
                destSyncOptions,
                paramResolverFunc);

        }

        /// <summary>
        ///     Deploy directory from different sources to different destiations using WebDeploy
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="destinationAddress"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="deleteExisting"></param>
        /// <param name="paramResolverFunc"></param>
        public static void DeployDirectory(
            string sourcePath,
            string destinationPath,
            string destinationAddress,
            string user,
            string password,
            bool deleteExisting = true,
            Func<string, string> paramResolverFunc = null)
        {
            InitializeWebDeployment();

            // define a source deployment provider and its properties
            const DeploymentWellKnownProvider srcProvider = DeploymentWellKnownProvider.ContentPath;
            var srcBaseOptions = new DeploymentBaseOptions();

            // define a destination deployment provider and its properties
            const DeploymentWellKnownProvider destProvider = DeploymentWellKnownProvider.ContentPath;
            var destBaseOptions = new DeploymentBaseOptions();

            // define a synchronization options and set if we shoudl delete existing files
            var destSyncOptions = new DeploymentSyncOptions { DoNotDelete = !deleteExisting };

            Deploy(
                sourcePath,
                destinationPath,
                destinationAddress,
                user,
                password,
                srcProvider,
                srcBaseOptions,
                destProvider,
                destBaseOptions,
                destSyncOptions,
                paramResolverFunc);
        }

        /// <summary>
        ///     Deploy package from different sources to different destiations using WebDeploy
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="destinationAddress"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="deleteExisting"></param>
        /// <param name="paramResolverFunc"></param>
        public static void DeployPackage(
            string sourcePath,
            string destinationPath,
            string destinationAddress,
            string user,
            string password,
            bool deleteExisting = true,
            Func<string, string> paramResolverFunc = null)
        {
            InitializeWebDeployment();

            var skipDirectives = new List<DeploymentSkipDirective>
                {
                    new DeploymentSkipDirective("skipDbFullSql", @"objectName=dbFullSql", true),
                    new DeploymentSkipDirective("skipDbMySql", @"objectName=dbMySql", true)
                };

            // define a source deployment provider and its properties
            const DeploymentWellKnownProvider srcProvider = DeploymentWellKnownProvider.Package;
            var srcBaseOptions = new DeploymentBaseOptions();

            // define a destination deployment provider and its properties
            const DeploymentWellKnownProvider destProvider = DeploymentWellKnownProvider.Auto;
            var destBaseOptions = new DeploymentBaseOptions();

            // define a synchronization options and set if we shoudl delete existing files
            var destSyncOptions = new DeploymentSyncOptions { DoNotDelete = !deleteExisting };

            Deploy(
                sourcePath,
                destinationPath,
                destinationAddress,
                user,
                password,
                srcProvider,
                srcBaseOptions,
                destProvider,
                destBaseOptions,
                destSyncOptions,
                paramResolverFunc,
                skipDirectives.AsEnumerable());
        }

        private static void Deploy(
            string sourcePath,
            string destinationPath,
            string destinationAddress, 
            string userName, 
            string password, 
            DeploymentWellKnownProvider srcProvider,
            DeploymentBaseOptions srcBaseOptions, 
            DeploymentWellKnownProvider destProvider,             
            DeploymentBaseOptions destBaseOptions, 
            DeploymentSyncOptions destSyncOptions,
            Func<string, string> syncParamResolver,
            IEnumerable<DeploymentSkipDirective> skipDirectives = null,
            IEnumerable<string> removedParameters = null, 
            TraceLevel tracelevel = TraceLevel.Info, 
            WebDeploySyncDirection direction = (WebDeploySyncDirection.SourceIsLocal | WebDeploySyncDirection.DestinationIsRemote))
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, e) => true;

            // prepare common source properties
            srcBaseOptions.Trace += DeployTraceEventHandler;
            srcBaseOptions.TraceLevel = tracelevel;
            if (direction.HasFlag(WebDeploySyncDirection.SourceIsRemote))
            {
                srcBaseOptions.ComputerName = destinationAddress;
                srcBaseOptions.UserName = userName;
                srcBaseOptions.Password = password;
                srcBaseOptions.AuthenticationType = "basic";
            }

            // prepare common destination properties
            destBaseOptions.Trace += DeployTraceEventHandler;
            destBaseOptions.TraceLevel = tracelevel;
            destBaseOptions.IncludeAcls = true;

            // We want to ignore errors to delete files because this is what WebMatrix does.  This may result in a partial deployment
            destBaseOptions.AddDefaultProviderSetting(DeploymentWellKnownProvider.FilePath.ToString(), "ignoreErrors", "0x80070005;0x80070020;0x80070091");
            destBaseOptions.AddDefaultProviderSetting(DeploymentWellKnownProvider.DirPath.ToString(), "ignoreErrors", "0x80070005;0x80070020;0x80070091");

            if (direction.HasFlag(WebDeploySyncDirection.DestinationIsRemote))
            {
                destBaseOptions.ComputerName = destinationAddress;
                destBaseOptions.UserName = userName;
                destBaseOptions.Password = password;
                destBaseOptions.AuthenticationType = "basic";
            }

            if (skipDirectives != null)
            {
                foreach (var skipDirective in skipDirectives)
                {
                    srcBaseOptions.SkipDirectives.Add(skipDirective);
                    destBaseOptions.SkipDirectives.Add(skipDirective);
                }
            }

            ExecuteDeploy(sourcePath, destinationPath, srcProvider, srcBaseOptions, destProvider, destBaseOptions, destSyncOptions, syncParamResolver, removedParameters);
        }

        private static void ExecuteDeploy(
            string sourcePath,
            string destinationPath,
            DeploymentWellKnownProvider srcProvider,
            DeploymentBaseOptions srcBaseOptions,
            DeploymentWellKnownProvider destProvider,
            DeploymentBaseOptions destBaseOptions,
            DeploymentSyncOptions destSyncOptions,
            Func<string, string> syncParamResolver = null,
            IEnumerable<string> removedParameters = null)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, e) => true;

            try
            {
                using (DeploymentObject deployObj = DeploymentManager.CreateObject(srcProvider, sourcePath, srcBaseOptions))
                {
                    // resolve parameters if any
                    if (syncParamResolver != null)
                    {
                        foreach (var syncParam in deployObj.SyncParameters)
                        {
                            var resolvedValue = syncParamResolver(syncParam.Name);
                            if (resolvedValue != null)
                            {
                                syncParam.Value = resolvedValue;
                            }
                        }
                    }

                    // remove parameters if any
                    if (removedParameters != null)
                    {
                        foreach (var parameter in removedParameters)
                        {
                            deployObj.SyncParameters.Remove(parameter);
                        }
                    }

                    TestEasyLog.Instance.Info(string.Format("Deploying: {0}", sourcePath));

                    // do action
                    TestEasyLog.Instance.LogObject(deployObj.SyncTo(destProvider, destinationPath, destBaseOptions, destSyncOptions));
                }
            }
            catch (Exception e)
            {
                TestEasyLog.Instance.Info(string.Format("Exception during deployment of '{0}': '{1}'", sourcePath, e.Message));
                throw;
            }
        }

        private static void DeployTraceEventHandler(object sender, DeploymentTraceEventArgs traceEvent)
        {
            TestEasyLog.Instance.Info("MsDeploy: " + traceEvent.Message);
        }
    }
}
