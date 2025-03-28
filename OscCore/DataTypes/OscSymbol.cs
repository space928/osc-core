// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

// ReSharper disable once CheckNamespace
namespace OscCore
{
    /// <summary>
    ///     Osc symbol
    /// </summary>
    /// <remarks>
    ///     Create a new symbol
    /// </remarks>
    /// <param name="value">literal string value</param>
    public readonly struct OscSymbol(string? value)
    {
        /// <summary>
        ///     The string value of the symbol
        /// </summary>
        public readonly string? Value = value;

        public override string? ToString() => Value;

        public override bool Equals(object obj)
        {
            if (Value == null) 
                return obj == null;

            return obj is OscSymbol symbol
                ? Value.Equals(symbol.Value)
                : Value.Equals(obj);
        }

        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
    }
}