// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;

// ReSharper disable once CheckNamespace
namespace OscCore
{
    /// <summary>
    ///     Osc Impulse Singleton
    /// </summary>
    public sealed class OscImpulse
    {
        public static readonly OscImpulse Value = new();

        private OscImpulse()
        {
        }

        /// <summary>
        ///     Matches the string against "Impulse", "Bang", "Infinitum", "Inf" the comparison is
        ///     StringComparison.OrdinalIgnoreCase
        /// </summary>
        /// <param name="str">string to check</param>
        /// <returns>true if the string matches any of the recognised impulse strings else false</returns>
        public static bool IsImpulse(ReadOnlySpan<char> str)
        {
            if (str.Length > 9)
                return false;

            Span<char> strLower = stackalloc char[str.Length];
            str.ToLowerInvariant(strLower);

            if (strLower.SequenceEqual("inf"))
                return true;
            if (strLower.SequenceEqual("bang"))
                return true;
            if (strLower.SequenceEqual("impulse"))
                return true;
            if (strLower.SequenceEqual("infinitum"))
                return true;

            return false;
        }

        public override string ToString()
        {
            return "impulse";
        }
    }
}