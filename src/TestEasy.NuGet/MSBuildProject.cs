using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TestEasy.NuGet
{
    /// <summary>
    /// IMSBuildProject implementation closely wrapping the Project type from Microsoft.Builder.Evaluation.
    /// </summary>
    [ExcludeFromCodeCoverage] // wrapper over external type
    class MsBuildProject : IMSBuildProject
    {
        Project project;

        public MsBuildProject(string projectFilePath)
        {
            var loadedProjects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFilePath);
            if (loadedProjects.Any())
            {
                project = loadedProjects.First();
            }
            else
            {
                // Use ToolsVersion="4.0" since TestEasy is compiled against .NET 4.0.  This is the same effect
                // that we would get with a roundtrippable project created in Dev11 or earlier.  Projects 
                // created in VS2013 and newer will use MSBuild 12.0 (and newer) by default, which will fail to 
                // build when we invoke MSBuild via our references to 4.0 (won't detect VS installations correctly).
                // In the future we may need to build TestEasy against a newer .NET framework version and 
                // reference (or specify) a newer ToolsVersion.
                project = new Project(projectFilePath, null, "4.0");
            }
        }

        public void Save()
        {
            project.Save();
        }

        public string FullPath
        {
            get { return project.FullPath; }
        }


        public void AddReference(string assemblyName)
        {
            throw new NotImplementedException();
        }

        public void AddReference(string assemblyName, string hintPath)
        {
            project.AddItem("Reference",
                            assemblyName,
                            new[] { 
                                    new KeyValuePair<string, string>("HintPath", hintPath)
                                });
        }

        public bool ReferenceExists(string assemblyName)
        {
            return ItemExists("Reference", assemblyName);
        }

        public bool ItemExists(string itemType, string name)
        {
            return project.GetItems(itemType).Where(pi => pi.EvaluatedInclude.Equals(name, StringComparison.OrdinalIgnoreCase)).Count() > 0;
        }

        public bool AnyItemExists(string name)
        {
            return project.Items.Where(pi => pi.EvaluatedInclude.Equals(name, StringComparison.OrdinalIgnoreCase)).Count() > 0;
        }

        public void RemoveItem(string itemType, string name)
        {
            if(ItemExists(itemType, name))
            {
                var projectItem = GetItem(itemType, name);
                project.RemoveItem(projectItem);
                Save();
            }
        }

        public string GetPropertyValue(string propertyName)
        {
            return project.GetPropertyValue(propertyName);
        }

        private ProjectItem GetItem(string itemType, string name)
        {
            return GetItems(itemType).Where(i => i.EvaluatedInclude.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        private ICollection<ProjectItem> GetItems(string itemType)
        {
            return project.GetItems(itemType);
        }

        public Dictionary<string, string> GetItemsWithMetadataProperty(string itemType, string metadataPropertyName)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            var items = project.GetItems(itemType).Where(i => i.HasMetadata(metadataPropertyName));

            foreach (var item in items)
            {
                results.Add(item.EvaluatedInclude, item.GetMetadataValue(metadataPropertyName));
            }

            return results;
        }
    }
}
