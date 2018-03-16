﻿// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig.Contracts
{
    using System.Collections.Generic;

    internal class HistoryResponse
    {
        public IEnumerable<History> History { get; set; }
    }
}
