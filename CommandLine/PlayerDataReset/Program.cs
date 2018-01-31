﻿// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace PlayerDataReset
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using CommandLine;
    using CommandLine.Text;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.PlayerReset;

    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            try
            {
                ResetOptions options = null;
                var parserResult = Parser.Default.ParseArguments<ResetOptions>(args)
                    .WithParsed(parsedOptions => options = parsedOptions);

                if (parserResult.Tag == ParserResultType.NotParsed)
                {
                    return -1;
                }

                return await OnReset(options);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: player data reset failed");
                Console.WriteLine(ex.Message);
                return -1;
            }
        }

        private static void PrintProviderDetails(List<JobProviderStatus> providers)
        {
            foreach (var provider in providers)
            {
                if (provider.Status == ResetProviderStatus.CompletedSuccess)
                {
                    Console.WriteLine($"\t{provider.Provider}, Status: {provider.Status} " +
                                      (provider.Status == ResetProviderStatus.CompletedSuccess? $"ErrorMsg: {provider.ErrorMessage}" : string.Empty));
                }
            }
        }

        private static async Task<int> OnReset(ResetOptions options)
        {
            if (options == null)
            {
                Console.WriteLine("Unknown parameter error");
                return -1;
            }

            DevAccount account = ToolAuthentication.LoadLastSignedInUser();
            if (account == null)
            {
                Console.Error.WriteLine("Didn't found dev sign in info, please use \"XblDevAccount.exe signin\" to initiate.");
                return -1;
            }

            Console.WriteLine($"Using Dev account {account.Name} from {account.AccountSource}");
            Console.WriteLine($"Resetting player {options.XboxUserId} data for scid {options.ServiceConfigurationId}, sandbox {options.Sandbox}");

            try
            {
                UserResetResult result = await PlayerReset.ResetPlayerDataAsync(
                    options.ServiceConfigurationId,
                    options.Sandbox, options.XboxUserId);

                switch (result.OverallResult)
                {
                    case ResetOverallResult.Succeeded:
                        Console.WriteLine("Resetting has completed successfully.");
                        return 0;
                    case ResetOverallResult.CompletedError:
                        Console.WriteLine("Resetting has completed with some error:");
                        PrintProviderDetails(result.ProviderStatus);
                        return -1;
                    case ResetOverallResult.Timeout:
                        Console.WriteLine("Resetting has timed out:");
                        PrintProviderDetails(result.ProviderStatus);
                        return -1;
                    default:
                        Console.WriteLine("has completed with unknown error");
                        return -1;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error: player data reset failed");

                if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Unauthorized)))
                {
                    Console.WriteLine(
                        $"Unable to authorize the account with XboxLive service with scid : {options.ServiceConfigurationId} and sandbox : {options.Sandbox}, please contact your administrator.");
                }
                else if (ex.Message.Contains(Convert.ToString((int) HttpStatusCode.Forbidden)))
                {
                    Console.WriteLine(
                        "Your account doesn't have access to perform the operation, please contact your administrator.");
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }

                return -1;
            }
        }

        internal class ResetOptions
        {
            [Option('c', "scid", Required = true,
                HelpText = "The service configuration ID (SCID) of the title for player data resetting")]
            public string ServiceConfigurationId { get; set; }

            [Option('s', "sandbox", Required = true,
                HelpText = "The target sandbox for player resetting")]
            public string Sandbox { get; set; }

            [Option('x', "xuid", Required = true,
                HelpText = "The Xbox Live user ID of the player to be reset")]
            public string XboxUserId { get; set; }

            [Usage(ApplicationAlias = "PlayerDataReset")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Reset a player for given scid and sandbox", new ResetOptions { ServiceConfigurationId = "xxx", Sandbox = "xxx", XboxUserId = "xxx" });
                }
            }
        }
    }
}
