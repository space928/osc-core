// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using OscCore.LowLevel;

// ReSharper disable once CheckNamespace
namespace OscCore
{
    /// <summary>
    ///     Represents a 32bit ARGB color
    /// </summary>
    /// <remarks>
    ///     This is a poor replacement for System.Drawing.Color but unfortunately many platforms do not support
    ///     the System.Drawing namespace.
    /// </remarks>
    public readonly struct OscColor
    {
        private const int AlphaShift = 0x18;
        private const int RedShift = 0x10;
        private const int GreenShift = 0x08;
        private const int BlueShift = 0;

        /// <summary>
        ///     Alpha, red, green and blue components packed into a single 32bit int
        /// </summary>
        public int ARGB { get; }

        /// <summary>
        ///     Red component
        /// </summary>
        public byte R => (byte)((ARGB >> RedShift) & 0xff);

        /// <summary>
        ///     Green component
        /// </summary>
        public byte G => (byte)((ARGB >> GreenShift) & 0xff);

        /// <summary>
        ///     Blue component
        /// </summary>
        public byte B => (byte)(ARGB & 0xff);

        /// <summary>
        ///     Alpha component
        /// </summary>
        public byte A => (byte)((ARGB >> AlphaShift) & 0xff);

        /// <summary>
        ///     Initate a new Osc-Color from an ARGB color value
        /// </summary>
        /// <param name="value">An 32bit ARGB integer</param>
        public OscColor(int value)
        {
            ARGB = value;
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                OscColor oscColor => oscColor.ARGB == ARGB,
                int intValue => intValue == ARGB,
                uint uintValue => unchecked((int)uintValue) == ARGB,
                _ => base.Equals(obj),
            };
        }

        public override string ToString()
        {
            //return $"{A}, {R}, {G}, {B}";
            return $"{R}, {G}, {B}, {A}";
        }

        public override int GetHashCode()
        {
            return ARGB;
        }

        /// <summary>
        ///     Create a Osc-Color from an 32bit ARGB integer
        /// </summary>
        /// <param name="argb">An ARGB integer</param>
        /// <returns>An Osc Color</returns>
        public static OscColor FromArgb(int argb)
        {
            return new OscColor(unchecked(argb & (int)0xffffffff));
        }

        /// <summary>
        ///     Create a Osc-Color from 4 channels
        /// </summary>
        /// <param name="alpha">Alpha channel component</param>
        /// <param name="red">Red channel component</param>
        /// <param name="green">Green channel component</param>
        /// <param name="blue">Blue channel component</param>
        /// <returns>An Osc Color</returns>
        public static OscColor FromArgb(
            int alpha,
            int red,
            int green,
            int blue)
        {
            CheckByte(alpha, "alpha");
            CheckByte(red, "red");
            CheckByte(green, "green");
            CheckByte(blue, "blue");

            return new OscColor(MakeArgb((byte)alpha, (byte)red, (byte)green, (byte)blue));
        }

        /// <inheritdoc cref="FromArgb(int, int, int, int)"/>
        public static OscColor FromArgb(byte alpha, byte red, byte green, byte blue)
        {
            return new OscColor(MakeArgb(alpha, red, green, blue));
        }

        private static int MakeArgb(
            byte alpha,
            byte red,
            byte green,
            byte blue)
        {
            return unchecked((int)((uint)((red << RedShift) | (green << GreenShift) | blue | (alpha << AlphaShift)) & 0xffffffff));
        }

        private static void CheckByte(int value, string name)
        {
            if (value >= 0 && value <= 0xff)
            {
                return;
            }

            throw new ArgumentException($"The {name} channel has a value of {value}, color channel values must be in the range 0 to {0xff}", name);
        }

        public static OscColor Parse(ref OscStringReader reader, IFormatProvider? provider)
        {
            ReadOnlySpan<char> strR, strG, strB, strA;

            OscSerializationToken token;

            reader.ReadNextToken(out var value);
            strR = value;
            reader.ReadNextToken(out _);

            reader.ReadNextToken(out value);
            strG = value;
            reader.ReadNextToken(out _);

            reader.ReadNextToken(out value);
            strB = value;
            reader.ReadNextToken(out _);

            reader.ReadNextToken(out value);
            strA = value;
            token = reader.ReadNextToken(out _);

            if (token != OscSerializationToken.ObjectEnd)
                throw new Exception("Invalid color");

            byte a, r, g, b;

            r = byte.Parse(strR.Trim(), NumberStyles.None, provider);
            g = byte.Parse(strG.Trim(), NumberStyles.None, provider);
            b = byte.Parse(strB.Trim(), NumberStyles.None, provider);
            a = byte.Parse(strA.Trim(), NumberStyles.None, provider);

            return FromArgb(a, r, g, b);
        }

        public static OscColor Parse(string str, IFormatProvider provider)
        {
            string[] pieces = str.Split(',');

            if (pieces.Length != 4)
            {
                throw new Exception($"Invalid color \'{str}\'");
            }

            byte a, r, g, b;

            r = byte.Parse(
                pieces[0]
                    .Trim(),
                NumberStyles.None,
                provider
            );
            g = byte.Parse(
                pieces[1]
                    .Trim(),
                NumberStyles.None,
                provider
            );
            b = byte.Parse(
                pieces[2]
                    .Trim(),
                NumberStyles.None,
                provider
            );
            a = byte.Parse(
                pieces[3]
                    .Trim(),
                NumberStyles.None,
                provider
            );

            return FromArgb(a, r, g, b);
        }
    }
}