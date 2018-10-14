TestEasy
========

### Overview

TestEasy is a set of packages that are designed to help automated tests (and not only) to manage various resources like web sites, web servers, Azure Resources, nuget packages, browsers etc. At this moment there are the following packages (assemblies):

-	**TestEasy.Core**: contains configuration logic, abstractions and helpers that other TestEasy packages depend on.
-	**TestEasy.WebServer**: contains helper classes that enable an easy way to execute basic operations for IIS and IISExpress )however can be extended to add support for other webservers)
-	**TestEasy.WebBrowser**: contains simple wrappers around Selenium web drivers and simplify local and remote browsers manipulation
-	**TestEasy.Azure**: contains wrappers for different Azure API (REST, managed etc) and provide a single entry point for Azure objects management 
-	**TestEasy.Nuget**: contains simplified logic for what VS does when it installs a nuget package to a web site or WAP, so you could manage nuget packages in your test websites at runtime from your code
-	**TestEasy.Full**: is a container package including all packages above

Each package is essentially a façade that uses some well-known APIs for different resources and provides simplified interface for end users to use in their code (all packages can be downloaded from www.nuget.org).

### Building TestEasy

Packages are targeting 4.5 .Net framework and you would need VS 2013 to open the solution. Solution can be built from VS or using _build.cmd_ file in the solution root folder. However, in order to have Bvt tests and samples working, please follow instructions in below sections, which describe how to initialize Dev machine once. 


### Setup instructions for Dev/Test/CI machines

Before using TestEasy in your test projects you would need to do following steps only once:
-	One time step: **prepare default.config** file and put it into shared folder **TestEasySupportPath** (read section _Initialize default.config_ for instructions)
-	One time step: **download tools** that TestEasy is relying and put into shared folder **TestEasySupportPath** (read section _Initialize TestEasy tools_ for instructions)

Then for each machine where tests are running:
-	On each machine: **create an environment variable TestEasySupportPath** and set its value to a path to the shared folder where you stored default.config and tools from two previous steps (read section _Initialize Dev/Test/CI machine_ for instructions)
This step should be done on each machine before running your tests (by some install step or script that prepares environment for your tests).

#### Configuration files

TestEasy packages are based on xml config files which can control runtime behaviour of your tests:
-	**Default.config**: a global file that normally should live somewhere in well-known location where all your test assemblies can access it in your network, for example some internal (for your organization) file share. This location we will refer as TestEasySupportPath. 
-	**Testsuite.config**: each test project (assembly) should have a testsuite.config file with specific to that project TestEasy settings, which would overwrite global settings in default.config file. Testsuite.config file should be copied to the bin folder where your project binaries will be generated.
-	**Context.config**: to vary TestEasy settings at runtime you may copy different context.config files to the bin folder of your test project and settings there will overwrite settings from default.config and testsuite.config. This is done to allow same test code to behave differently at runtime depending on your test environment.

All those config files will be mapped together via standard .Net configuration map (similar to master.config and web.configs etc), so context.config overwrites testsuite.config, which in turn overwrites default.config.
You may have 2 user scenarios:
-	Create a default.config file once and put it into some shared folder. In default.config file you store some global settings that all your test projects will refer to.
-	Add all settings to your projects' testsuite.config file, however a drawback here is that you would have to repeat all settings in testsuite.config files for each project.


#### Initialize default.config

First read section _Configuration files_ to understand how they are organized.
Please follow these steps:
-	Choose your shared folder that is visible to all your tests from all test machines in your network, let’s call it **TestEasySupportPath**.
-	Take default.config template file that can be found in TestEasy.Core\Configuration folder and copy it to **TestEasySupportPath**.
-	Substitute missing tokens there with correct values that make sense in your network:
 - **<tools defaultRemoteToolsPath**= put here some shared folder where you will copy tools (see section Preparing TestEasy tools below), usually it can be same **TestEasySupportPath** folder.
 - **<client remoteHubUrl**= http://[YOUR-MACHINE-NAME]:4444/wd/hub  - substitute with your Selenium Grid hub machine name (and port if needed). Please see a section about setting up a Selenium grid hub and node machines. This setting is used if you would need to have remote browsers to request your test websites. In basic scenario you would have all browsers to be installed on your test/dev machine locally, however you may choose to run tests on remote browsers to avoid browser windows opening and closing on your dev machine during tests execution.
 - **azure/subscriptions** add at least one subscription information if you plan to use TestEasy.Azure package. Subscriptions collection contains information about subscriptions, where each subscription is represented with:
    -	**Alias name** not real subscription name, but some kind of alias that makes sense to you inside your tests and is safe to be checked in with your code
    -	**publishSettingsFile** path to the file .publishsettings that can be downloaded from Azure portal using _Get-AzurePublishSettingsFile_ powershell command (or through portal UI). (See this web page for details about _Get-AzurePublishSettingsFile_: http://msdn.microsoft.com/en-us/library/dn495224.aspx .) _Note: if you have several subscriptions in your .publishsettings file, please make sure that you left only one there, i.e. separate all subscriptions info different .publishsettings files so TestEasy could easily understand which file corresponds to which alias name_. _Note: Store your .publishsettings files under your internal shared folder **TestEasySupportPath** for example, don’t check them in with your tests since they contain private information about your subscription_
    -	**<azure defaultSubscription** – should contain an alias for one of your subscription in <subscriptions> collection and will be used by default when you don’t specify subscription explicitly in the code (for example new Subscription() would use that setting, since you did not provide an alias to the constructor)

#### Initialize TestEasy tools 

By “tools” TestEasy means any executable or msi file that need to be executed at runtime once or multiple times during TestEasy API calls. At this moment TestEasy only need “tools” for TestEasy.WebBrowser assembly to run smoothly with Selenium and Selenium Grid. As you could see from default.config (template default.config file could be found in TestEasy.Core\Configuration folder), following files need to be copied once(!) to shared folder which we call **TestEasySupportPath**:
-	Selenium-server-standalone-xxxx.jar
-	IEDriverServer.exe
-	ChromeDriver.exe
-	Jre-7-windows-i586.exe
-	Jre-7-windows-x64.exe

You could download those files on your own, however for convenience there is a powershell script that will do it for you:
_powershell -ExecutionPolicy Bypass [YOUR-LOCAL-PATH]\DownloadTestEasyTools.ps1 -destinationFolder "[YOUR-FOLDER]"_
where,
-	[YOUR-LOCAL-PATH] is your repo-root\scripts
-	[YOUR-FOLDER] is the folder where those files should be downloaded, i.e. **TestEasySupportPath**

After you are done with preparing default.config and TestEasy tools your **TestEasySupportPath** shared folder should have following files:
-	Selenium-server-standalone-xxxx.jar
-	IEDriverServer.exe
-	ChromeDriver.exe
-	Jre-7-windows-i586.exe
-	Jre-7-windows-x64.exe
-	Default.config

You had to do it only once and now TestEasy APIs from any machine should be able to use this folder seamlessly. The only time when you need to update something in that folder is when new version of tool is adopted by TestEasy or if you want to do any changes to default.config.

#### Initialize Dev/Test/CI machine

In order to run TestEasy tests (Bvts or samples) on your dev/test/ci machine you need to do this last step. Assuming that you completed steps above, create an environment variable on your Dev/Test/CI machine 

	TestEasySupportPath=[PATH] 
	
and set its value to the path of your shared folder that you prepared earlier. For dynamically created test machines you may want to create a script that will create this environment variable for you after machine is reimaged



