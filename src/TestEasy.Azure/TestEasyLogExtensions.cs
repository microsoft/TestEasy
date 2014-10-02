using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Web.Deployment;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Models;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using TestEasy.Azure.Helpers;
using TestEasy.Core;

namespace TestEasy.Azure
{
    internal static class TestEasyLogExtensions
    {
        internal static void LogObject(this TestEasyLog log, DeploymentChangeSummary changeSummary)
        {
            log.Info(string.Format("Deployment Results \n  Added: {0}\n  Updated: {1}\n  Deleted: {2}\n  Total Errors: {3}\n  Total Changes: {4}",
                    changeSummary.ObjectsAdded,
                    changeSummary.ObjectsUpdated,
                    changeSummary.ObjectsDeleted,
                    changeSummary.Errors,
                    changeSummary.TotalChanges));
        }

        internal static void LogObject(this TestEasyLog log, DeploymentGetResponse deployment)
        {
            if (deployment == null) return;

            log.Info(string.Format("Name:{0}", deployment.Name));
            log.Info(string.Format("Label:{0}", Base64EncodingHelper.DecodeFromBase64String(deployment.Label)));
            log.Info(string.Format("Url:{0}", deployment.Uri));
            log.Info(string.Format("Status:{0}", deployment.Status));
            log.Info(string.Format("DeploymentSlot:{0}", deployment.DeploymentSlot));
            log.Info(string.Format("PrivateID:{0}", deployment.PrivateId));
            log.Info(string.Format("UpgradeDomainCount:{0}", deployment.UpgradeDomainCount));

            LogObject(log, deployment.Roles);
            LogObject(log, deployment.RoleInstances);
            LogObject(log, deployment.UpgradeStatus);
        }

        internal static void LogObject(this TestEasyLog log, IList<Role> roleList)
        {
            if (roleList == null) return;

            log.Info(string.Format("RoleList contains {0} item(s).", roleList.Count));
            foreach (var r in roleList)
            {
                log.Info(string.Format("    RoleName: {0}", r.RoleName));
                log.Info(string.Format("    OperatingSystemVersion : {0}", r.OSVersion));
            }
        }

        internal static void LogObject(this TestEasyLog log, IList<RoleInstance> roleInstanceList)
        {
            if (roleInstanceList == null) return;

            log.Info(string.Format("RoleInstanceList contains {0} item(s).", roleInstanceList.Count));
            foreach (var obj in roleInstanceList)
            {
                log.Info(string.Format("    RoleName: {0}", obj.RoleName));
                log.Info(string.Format("    InstanceName: {0}", obj.InstanceName));
                log.Info(string.Format("    InstanceStatus: {0}", obj.InstanceStatus));
            }
        }

        internal static void LogObject(this TestEasyLog log, UpgradeStatus upgradeStatus)
        {
            if (upgradeStatus == null) return;

            log.Info(string.Format("UpgradeType: {0}", upgradeStatus.UpgradeType));
            log.Info(string.Format("CurrentUpgradeDomain: {0}", upgradeStatus.CurrentUpgradeDomain));
            log.Info(string.Format("CurrentUpgradeDomainState: {0}", upgradeStatus.CurrentUpgradeDomainState));
        }

        internal static void LogObject(this TestEasyLog log, HostedServiceListResponse.HostedService hostedService)
        {
            if (hostedService == null) return;

            if (!string.IsNullOrEmpty(hostedService.ServiceName))
            {
                log.Info(string.Format("HostedService Name:{0}", hostedService.ServiceName));
            }

            log.Info(string.Format("HostedService Url:{0}", hostedService.Uri));

            LogObject(log, hostedService.Properties);
        }

        internal static void LogObject(this TestEasyLog log, HostedServiceProperties hostedServiceProperties)
        {
            if (hostedServiceProperties == null) return;

            log.Info(string.Format("HostedService Label:{0}", Base64EncodingHelper.DecodeFromBase64String(hostedServiceProperties.Label)));
            log.Info(string.Format("HostedService Description:{0}", hostedServiceProperties.Description));

            if (!string.IsNullOrEmpty(hostedServiceProperties.AffinityGroup))
            {
                log.Info(string.Format("HostedService AffinityGroupName:{0}", hostedServiceProperties.AffinityGroup));
            }

            if (!string.IsNullOrEmpty(hostedServiceProperties.Location))
            {
                log.Info(string.Format("HostedService Location:{0}", hostedServiceProperties.Location));
            }
        }

        internal static void LogObject(this TestEasyLog log, StorageAccount storageService)
        {
            if (storageService == null) return;

            if (!string.IsNullOrEmpty(storageService.Name))
            {
                log.Info(string.Format("StorageService Name:{0}", storageService.Name));
            }

            log.Info(string.Format("StorageService Url:{0}", storageService.Uri));
        }

        internal static void LogObject(this TestEasyLog log, AffinityGroupListResponse.AffinityGroup affinityGroup)
        {
            if (affinityGroup == null) return;

            log.Info(string.Format("AffinityGroup Name:{0}", affinityGroup.Name));
            if (!string.IsNullOrEmpty(affinityGroup.Label))
            {
                log.Info(string.Format("AffinityGroup Label:{0}", affinityGroup.Label));
            }

            log.Info(string.Format("AffinityGroup Description:{0}", affinityGroup.Description));
            log.Info(string.Format("AffinityGroup Location:{0}", affinityGroup.Location));
        }

        internal static void LogObject(this TestEasyLog log, VirtualMachineDiskListResponse.VirtualMachineDisk vhd)
        {
            if(vhd == null) return;

            log.Info(string.Format("VHD Name: {0}", vhd.Name));
            log.Info(string.Format("VHD Uri: {0}", vhd.MediaLinkUri));
            log.Info(string.Format("VHD Size: {0}GB", vhd.LogicalSizeInGB));
        }

        internal static void LogObject(this TestEasyLog log, VirtualMachineOSImageListResponse.VirtualMachineOSImage osImage)
        {
            if (osImage == null) return;

            log.Info(string.Format("OSImage Name: {0}", osImage.Name));
            log.Info(string.Format("OSImage Operating System: {0}", osImage.OperatingSystemType));
            if (!string.IsNullOrEmpty(osImage.RecommendedVMSize))
            {
                log.Info(string.Format("OSImage Recommended Size: {0}", osImage.RecommendedVMSize));
            }
        }

        internal static void LogObject(this TestEasyLog log, WebSite.WebSiteSslCertificate certificate)
        {
            if (certificate == null) return;

            if (certificate.SelfLinkUri != null)
            {
                log.Info(string.Format("Certificate Url:{0}", certificate.SelfLinkUri));
            }

            if (certificate.Thumbprint != null)
            {
                log.Info(string.Format("Certificate Thumbprint:{0}", certificate.Thumbprint));
            }

            if (certificate.PfxBlob != null)
            {
                X509Certificate2 cert = null;
                if(String.IsNullOrEmpty(certificate.Password))
                {
                    cert = new X509Certificate2(certificate.PfxBlob);
                }
                else
                {
                    cert = new X509Certificate2(certificate.PfxBlob, certificate.Password);
                }

                log.Info(string.Format("Certificate FriendlyName:{0}", cert.FriendlyName));
                log.Info(string.Format("Certificate Subject:{0}", cert.Subject));
                log.Info(string.Format("Certificate Issuer:{0}", cert.Issuer));
                log.Info(string.Format("Certificate SerialNumber:{0}", cert.SerialNumber));
                log.Info(string.Format("Certificate Data:{0}", certificate.PfxBlob));
            }
        }

        internal static void LogObject(this TestEasyLog log, IList<StorageAccount> storageServiceList)
        {
            if (storageServiceList == null) return;

            log.Info(string.Format("StorageServiceList contains {0} item(s).", storageServiceList.Count));
            foreach (var item in storageServiceList)
            {
                LogObject(log, item);
            }
        }

        internal static void LogObject(this TestEasyLog log, IList<AffinityGroupListResponse.AffinityGroup> affinityGroupList)
        {
            if (affinityGroupList == null) return;

            log.Info(string.Format("AffinityGroupList contains {0} item(s).", affinityGroupList.Count));
            foreach (var item in affinityGroupList)
            {
                LogObject(log, item);
            }
        }

        internal static void LogObject(this TestEasyLog log, IList<HostedServiceListResponse.HostedService> hostedServiceList)
        {
            if (hostedServiceList == null) return;

            log.Info(string.Format("HostedServiceList contains {0} item(s).", hostedServiceList.Count));
            foreach (var item in hostedServiceList)
            {
                LogObject(log, item);
            }
        }

        internal static void LogObject(this TestEasyLog log, IList<DeploymentGetResponse> deploymentList)
        {
            if (deploymentList == null) return;

            log.Info(string.Format("DeploymentList contains {0} item(s).", deploymentList.Count));
            foreach (var item in deploymentList)
            {
                LogObject(log, item);
            }
        }

        internal static void LogObject(this TestEasyLog log, IList<WebSite.WebSiteSslCertificate> certificateList)
        {
            if (certificateList == null) return;

            log.Info(string.Format("CertificateList contains {0} item(s).", certificateList.Count));
            foreach (var item in certificateList)
            {
                LogObject(log, item);
            }
        }

        internal static void LogObject(this TestEasyLog log, IList<VirtualMachineDiskListResponse.VirtualMachineDisk> vhdList)
        {
            if (vhdList == null) return;

            log.Info(string.Format("VHD List contains {0} item(s).", vhdList.Count));
            foreach (var item in vhdList)
            {
                LogObject(log, item);
            }
        }

        internal static void LogObject(this TestEasyLog log, IList<VirtualMachineOSImageListResponse.VirtualMachineOSImage> osImageList)
        {
            if (osImageList == null) return;

            log.Info(string.Format("OSImageList contains {0} item(s).", osImageList.Count));
            foreach (var item in osImageList)
            {
                LogObject(log, item);
            }
        }
    }
}
