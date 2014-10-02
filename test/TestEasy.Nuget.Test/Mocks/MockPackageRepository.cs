using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestEasy.Nuget.Test.Mocks
{
    class MockPackageRepository : IPackageRepository
    {
        Dictionary<string, List<IPackage>> installedPackages = new Dictionary<string, List<IPackage>>();

        #region Test Helpers
        public string FindLatestVersion(string packageId)
        {
            if (installedPackages.ContainsKey(packageId))
            {
                return installedPackages[packageId].OrderByDescending(p => p.Version).First().Version.ToString();
            }
            return null;
        }

        #endregion

        public void AddPackage(IPackage package)
        {
            if (!installedPackages.ContainsKey(package.Id))
            {
                installedPackages.Add(package.Id, new List<IPackage>());
            }
            if (installedPackages[package.Id].Where(p => p.Version == package.Version).Count() == 0)
            {
                installedPackages[package.Id].Add(package);
            }
        }

        public IQueryable<IPackage> GetPackages()
        {
            return installedPackages.Values.Aggregate(new List<IPackage>(), (a, l) => { a.AddRange(l); return a; }, (a) => a).AsQueryable<IPackage>();
        }

        public void RemovePackage(IPackage package)
        {
            installedPackages[package.Id].Remove(package);
        }

        public string Source
        {
            get { return string.Empty; }
        }

        public bool SupportsPrereleasePackages
        {
            get { return false; }
        }

        public PackageSaveModes PackageSaveMode
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
