﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OfficeProPlus.Downloader.Model;
using Microsoft.OfficeProPlus.InstallGenerator.Model;
using Microsoft.OfficeProPlus.Downloader;
using Microsoft.Win32;
using System.Management;
namespace Microsoft.OfficeProPlus.InstallGenerator.Implementation
{
    class OfficeWmiInstallManager : IManageOfficeInstall
    {

        public string remoteUser { get; set;}
        public string remoteComputerName { get; set;}
        public string remoteDomain { get; set;}
        public string remotePass { get; set;}

        public ManagementScope scope { get; set;}


        public  async Task initConnection()
        {

           
            var computerName = remoteComputerName;
            var password = remotePass;

            var timeOut = new TimeSpan(0, 5, 0);
            ConnectionOptions options = new ConnectionOptions();
            options.Authority = "NTLMDOMAIN:" + remoteDomain;
            options.Username = remoteUser;
            options.Password = remotePass;
            options.Impersonation = ImpersonationLevel.Impersonate;
            options.Timeout = timeOut;



            scope = new ManagementScope("\\\\" + remoteComputerName + "\\root\\cimv2", options);
            scope.Options.EnablePrivileges = true;

            try
            {
                scope.Connect();
            }
            catch (Exception)
            {
              
                scope.Connect();
            }

            await CheckForOfficeInstallAsync();




        }

        public async Task<OfficeInstallation> CheckForOfficeInstallAsync()
        {

         
                var officeInstance = new OfficeInstallation() { Installed = false };
                var officeRegPathKey = @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration";


                officeInstance.Version = await Task.Run(() => { return GetRegistryValue(officeRegPathKey, "VersionToReport", "GetStringValue"); });
                
                if(string.IsNullOrEmpty(officeInstance.Version))
                {
                    officeRegPathKey = @"SOFTWARE\Microsoft\Office\16.0\ClickToRun\Configuration";
                    officeInstance.Version = await Task.Run(() => { return GetRegistryValue(officeRegPathKey, "VersionToReport", "GetStringValue"); });

                    if (string.IsNullOrEmpty(officeInstance.Version))
                    {
                        officeRegPathKey = @"SOFTWARE\Microsoft\Office\15.0\ClickToRun\Configuration";
                        officeInstance.Version = await Task.Run(() => { return GetRegistryValue(officeRegPathKey, "VersionToReport", "GetStringValue"); });

                    }

                }

                if(!string.IsNullOrEmpty(officeInstance.Version))
                {
                    officeInstance.Installed = true; 
                    var currentBaseCDNUrl = await Task.Run(() => { return GetRegistryValue(officeRegPathKey, "CDNBaseUrl", "GetStringValue"); });


                    var installFile = await GetOfficeInstallFileXml();
                    if (installFile == null) return officeInstance;

                    var currentBranch = installFile.BaseURL.FirstOrDefault(b => b.URL.Equals(currentBaseCDNUrl) &&
                                                                          !b.Branch.ToLower().Contains("business"));
                    if (currentBranch != null)
                    {
                        officeInstance.Channel = currentBranch.Branch;

                        var latestVersion = await GetOfficeLatestVersion(currentBranch.Branch, OfficeEdition.Office32Bit);
                        officeInstance.LatestVersion = latestVersion;
                    }


            }




            return officeInstance;
            
        }


        public async Task<UpdateFiles> GetOfficeInstallFileXml()
        {
            var ppDownload = new ProPlusDownloader();
            var installFiles = await ppDownload.DownloadCabAsync();
            if (installFiles != null)
            {
                var installFile = installFiles.FirstOrDefault();
                if (installFile != null)
                {
                    return installFile;
                }
            }
            return null;
        }

        private Task<string> GenerateConfigXml()
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetOfficeLatestVersion(string branch, OfficeEdition edition)
        {
            var ppDownload = new ProPlusDownloader();
            var latestVersion = await ppDownload.GetLatestVersionAsync(branch, edition);
            return latestVersion;
        }


        public string GetRegistryValue(RegistryKey regKey, string property)
        {
            throw new NotImplementedException();
        }

        private string GetRegistryValue(string regKey, string valueName, string getmethParam)
        {
            var regValue = "";

            ManagementClass registry = new ManagementClass(scope, new ManagementPath("StdRegProv"), null);
            ManagementBaseObject inParams = registry.GetMethodParameters(getmethParam);

            inParams["hDefKey"] = 0x80000002;
            inParams["sSubKeyName"] = regKey;
            inParams["sValueName"] = valueName;

            ManagementBaseObject outParams = registry.InvokeMethod(getmethParam, inParams, null);

            if(outParams.Properties["sValue"].Value.ToString() != null)
            {
                regValue = outParams.Properties["sValue"].Value.ToString();
            }


            return regValue; 
        }
        public void UninstallOffice(string installVer = "2016")
        {
            throw new NotImplementedException();
        }

        public Task UpdateOffice()
        {
            throw new NotImplementedException();
        }
    }
}
