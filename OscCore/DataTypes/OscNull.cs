// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;

// ReSharper disable once CheckNamespace
namespace OscCore
{
    /// <summary>
    ///     Osc Null Singleton
    /// </summary>
    public sealed class OscNull
    {
        public static readonly OscNull Value = new();

        private OscNull()
        {
        }

        public static bool IsNull(ReadOnlySpan<char> str)
        {
            if (str.Length > 4)
                return false;

            Span<char> strLower = stackalloc char[str.Length];
            str.ToLowerInvariant(strLower);

            if (strLower.SequenceEqual("null"))
                return true;
            return strLower.SequenceEqual("nil");
        }

        public override string ToString()
        {
            return "null";
        }
    }
}