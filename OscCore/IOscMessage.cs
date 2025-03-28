// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;
using System.Net;

namespace OscCore
{
    public interface IOscMessage
    {
        string? Address { get; }

        int Count { get; }

        IPEndPoint? Origin { get; }

        OscTimeTag? Timestamp { get; }
    }
}