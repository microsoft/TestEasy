TestEasy
========

TestEasy is a set of packages that are designed to help automated tests (and not only) to manage various resources like web sites, web servers, Azure Resources, nuget packages, browsers etc. At this moment there are following packages (assemblies):

-	**TestEasy.Core**: contains configuration logic, abstractions and helpers that other TestEasy packages depend on.
-	**TestEasy.WebServer**: contains helper classes that enable an easy way to execute basic operations for IIS and IISExpress )however can be extended to add support for other webservers)
-	**TestEasy.WebBrowser**: contains simple wrappers around Selenium web drivers and simplify local and remote browsers manipulation
-	**TestEasy.Azure**: contains wrappers for different Azure API (REST, managed etc) and provide a single entry point for Azure objects management 
-	**TestEasy.Nuget**: contains simplified logic for what VS does when it installs a nuget package to a web site or WAP, so you could manage nuget packages in your test websites at runtime from your code
-	**TestEasy.Full**: is a container package including all packages above

Each package is essentially a façade that uses some well-known APIs for different resources and provides simplified interface for end users to use in their code.
All packages can be downloaded from www.nuget.org.
