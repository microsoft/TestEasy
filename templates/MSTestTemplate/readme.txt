Create a new project/solution
=============================
- Create new project on your local machine
- Go to Nuget Settings and add a custom feed: \\fc-fileserv\FxtTestTools\InternalNugetFeed
- Install all packages needed for your tests : TestEasy + any other packages that you need
- Build

Now you have project working on your local machine


Make sure solution builds everywhere
====================================

After you completed steps above, project has all references, however when some one else enlists and builds 
your project, you need to make sure that they can build it correctly. In order to do that we need automatically
set custom nuget feed in our build targets:

- Since you project reference TestEasy assemblies you need to install TestEasy.Nuget packages which 
live under internal Nuget feed (shared folder): \\fc-fileserv\FxtTestTools\InternalNugetFeed. Notice, that 
you still need official nuget feed to install some dependencies. In order to register local and official 
nuget feeds, you need to edit NuGet.targets file and add/replace following section at the top:

    <ItemGroup Condition=" '$(PackageSources)' == '' ">
        <!-- Package sources used to restore packages. By default will used the registered sources under %APPDATA%\NuGet\NuGet.Config -->
            <PackageSource Include="https://nuget.org/api/v2/" />
            <PackageSource Include="\\fc-fileserv\FxtTestTools\InternalNugetFeed" />        
    </ItemGroup>

- NuGet.targets lives under .nuget folder in your solution normally and your project should have this import 
    <Import Project="$(SolutionDir)\.nuget\nuget.targets" />. 

- Make sure that your new project has this property set for all configurations: 
    <RestorePackages>true</RestorePackages>.

- If you have MSTest project and have pre-created websites that your tests expect at runtime, you need to add an AfterBuild step
to copy your websites to bin\debug:

	<Exec Command="xcopy /Y /E &quot;$(ProjectDir)..\WebSites\*.*&quot; &quot;$(TargetDir)WebSites\*.*&quot;" />

Then in your test you would define [DeploymentItem("SampleWebSites")] attribute which at runtime would find SampleWebSites folder 
here bin\debug\SamplesWebSites and copy its contents to "ShadowCopyFolder"\TestResults\Out\*.*. So your websites will live under Out 
folder, which will be "current" at runtime.

All that should be done only once when solution is being created.
