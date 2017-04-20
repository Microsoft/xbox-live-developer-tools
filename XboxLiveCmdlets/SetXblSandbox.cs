﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace XboxLiveCmdlet
{
    using System;
    using System.Management.Automation;
    using System.ServiceProcess;
    using Microsoft.Tools.WindowsDevicePortal;
    using Microsoft.Win32;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    [Cmdlet(VerbsCommon.Set, "XblSandbox")]
    public class SetXblSandbox : XboxliveCmdlet
    {
        public string MachineName { get; set; }

        [Parameter(Mandatory = true)]
        public string SandboxId { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrEmpty(MachineName))
                {
                    // Check if running as admin
                    // Get the ID and security principal of the current user account
                    var currentId = System.Security.Principal.WindowsIdentity.GetCurrent();
                    var currentPricipal = new System.Security.Principal.WindowsPrincipal(currentId);

                    //Get the security principal for the administrator role
                    if (currentPricipal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                    {
                        // To avoid registry being redirected to wow6432, open from registry64
                        RegistryKey regBase = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                        var xboxLiveKey = regBase.CreateSubKey(@"SOFTWARE\Microsoft\XboxLive");
                        xboxLiveKey.SetValue("Sandbox", SandboxId);

                        //Stop authman if it's running. It will auto start on next auth call.
                        ServiceController service = new ServiceController("XblAuthManager");
                        TimeSpan timeout = TimeSpan.FromSeconds(30);

                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        }
                    }
                    else
                    {
                        // launch a elevated powershell session and re-call the command 
                        var assembly = Assembly.GetExecutingAssembly();

                        string command = "Import-Module " + assembly.Location + ";";
                        command += this.MyInvocation.Line;

                        // launch powershell in admin
                        var newProcessInfo = new System.Diagnostics.ProcessStartInfo("Powershell");
                        newProcessInfo.Arguments = $"-command \" &{{ {command} }}";
                        newProcessInfo.Verb = "runas";

                        var process = System.Diagnostics.Process.Start(newProcessInfo);
                        process.WaitForExit();
                    }

                    // Get sandbox 
                    WriteObject(GetXblSandbox.GetLocalSandboxObject());
                }
                else
                {
                    string url = "https://" + MachineName + ":11443";
                    IDevicePortalConnection connection = new DefaultDevicePortalConnection(url, string.Empty, string.Empty);
                    DevicePortal portal = new DevicePortal(connection);

                    portal.SetXboxLiveSandboxAsync(SandboxId).Wait();
                    portal.RebootAsync().Wait();
                }
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                WriteError(new ErrorRecord(innerEx, "SetXblSandbox failed: " + innerEx.Message, ErrorCategory.InvalidOperation, null));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "SetXblSandbox failed: " + ex.Message, ErrorCategory.InvalidOperation, null));
            }
        }

    }
}
