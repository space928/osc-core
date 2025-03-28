﻿// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OscCore.LowLevel
{
    public class OscSerializationUtils
    {
        /// <summary>
        ///     Turn a byte array into a readable, escaped string
        /// </summary>
        /// <param name="original">bytes</param>
        /// <returns>a string</returns>
        public static string Escape(string original)
        {
            // the result is maximum of bytes length * 4
            char[] chars = new char[original.Length * 4];

            int j = 0;

            for (int i = 0; i < original.Length; i++)
            {
                char c = original[i];

                if (c > '~')
                {
                    //chars[j++] = '�';
                    chars[j++] = '\\';
                    chars[j++] = 'x';
                    chars[j++] = ((c & 240) >> 4).ToString("X")[0];
                    chars[j++] = (c & 15).ToString("X")[0];
                }
                else
                {
                    switch (c)
                    {
                        case '\0':
                            chars[j++] = '\\';
                            chars[j++] = '0';
                            break;

                        case '\a':
                            chars[j++] = '\\';
                            chars[j++] = 'a';
                            break;

                        case '\b':
                            chars[j++] = '\\';
                            chars[j++] = 'b';
                            break;

                        case '\f':
                            chars[j++] = '\\';
                            chars[j++] = 'f';
                            break;

                        case '\n':
                            chars[j++] = '\\';
                            chars[j++] = 'n';
                            break;

                        case '\r':
                            chars[j++] = '\\';
                            chars[j++] = 'r';
                            break;

                        case '\t':
                            chars[j++] = '\\';
                            chars[j++] = 't';
                            break;

                        case '\v':
                            chars[j++] = '\\';
                            chars[j++] = 'v';
                            break;

                        case '"':
                            chars[j++] = '\\';
                            chars[j++] = '"';
                            break;

                        case '\\':
                            chars[j++] = '\\';
                            chars[j++] = '\\';
                            break;

                        default:
                            if (c >= ' ')
                            {
                                chars[j++] = c;
                            }
                            else
                            {
                                chars[j++] = '\\';
                                chars[j++] = 'x';
                                chars[j++] = ((c & 240) >> 4).ToString("X")[0];
                                chars[j++] = (c & 15).ToString("X")[0];
                            }

                            break;
                    }
                }
            }

            return new string(chars, 0, j);
        }

        public static bool IsValidEscape(string str)
        {
            bool isEscaped = false;
            bool parseHexNext = false;
            int parseHexCount = 0;

            // first we count the number of chars we will be returning
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (parseHexNext)
                {
                    parseHexCount++;

                    if (IsHexChar(c) == false)
                    {
                        return false; 
                    }
//
//                    if (Uri.IsHexDigit(c) == false)
//                    {
//                        return false;
//                    }

                    if (parseHexCount == 2)
                    {
                        parseHexNext = false;
                        parseHexCount = 0;
                    }
                }
                // if we are not in  an escape sequence and the char is a escape char
                else if (isEscaped == false && c == '\\')
                {
                    // escape
                    isEscaped = true;
                }
                // else if we are escaped
                else if (isEscaped)
                {
                    // reset escape state
                    isEscaped = false;

                    // check the char against the set of known escape chars
                    switch (char.ToLower(c))
                    {
                        case '0':
                        case 'a':
                        case 'b':
                        case 'f':
                        case 'n':
                        case 'r':
                        case 't':
                        case 'v':
                        case '"':
                        case '\\':
                            // do not increment count
                            break;

                        case 'x':
                            // do not increment count
                            parseHexNext = true;
                            parseHexCount = 0;
                            break;

                        default:
                            // this is not a valid escape sequence
                            // return false
                            return false;
                    }
                }
            }

            if (parseHexNext)
            {
                return false;
            }

            return isEscaped == false;
        }

        public static string ToStringBlob(byte[] bytes)
        {
            // if the default is to be Base64 encoded
            return "64x" + System.Convert.ToBase64String(bytes);

//            StringBuilder sb = new StringBuilder(bytes.Length * 2 + 2);
//
//            sb.Append("0x");
//
//            foreach (byte b in bytes)
//            {
//                sb.Append(b.ToString("X2"));
//            }
//
//            return sb.ToString();
        }

        /// <summary>
        ///     Turn a readable string into a byte array
        /// </summary>
        /// <param name="str">a string, optionally with escape sequences in it</param>
        /// <returns>a byte array</returns>
        public static string Unescape(ReadOnlySpan<char> str)
        {
            int count = 0;
            bool isEscaped = false;
            bool parseHexNext = false;
            int parseHexCount = 0;

            // Uri.HexEscape(
            // first we count the number of chars we will be returning
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (parseHexNext)
                {
                    parseHexCount++;

                    if (IsHexChar(c) == false)
                        throw new Exception($@"Invalid escape sequence at char '{i}' ""{c}"" is not a valid hex digit.");

                    if (parseHexCount == 2)
                    {
                        parseHexNext = false;
                        parseHexCount = 0;
                    }
                }
                // if we are not in  an escape sequence and the char is a escape char
                else if (isEscaped == false && c == '\\')
                {
                    // escape
                    isEscaped = true;

                    // increment count
                    count++;
                }
                // else if we are escaped
                else if (isEscaped)
                {
                    // reset escape state
                    isEscaped = false;

                    // check the char against the set of known escape chars
                    switch (char.ToLower(c))
                    {
                        case '0':
                        case 'a':
                        case 'b':
                        case 'f':
                        case 'n':
                        case 'r':
                        case 't':
                        case 'v':
                        case '"':
                        case '\\':
                            // do not increment count
                            break;

                        case 'x':
                            // do not increment count
                            parseHexNext = true;
                            parseHexCount = 0;
                            break;

                        default:
                            // this is not a valid escape sequence
                            throw new Exception($"Invalid escape sequence at char '{i - 1}'.");
                    }
                }
                else
                {
                    // normal char increment count
                    count++;
                }
            }

            if (parseHexNext)
                throw new Exception($"Invalid escape sequence at char '{str.Length - 1}' missing hex value.");

            if (isEscaped)
                throw new Exception($"Invalid escape sequence at char '{str.Length - 1}'.");
            
            // reset the escape state
            //            isEscaped = false;
            //            parseHexNext = false;
            //            parseHexCount = 0;

            // create a byte array for the result
            Span<char> chars = stackalloc char[0];
            if (count <= 1024)
                chars = stackalloc char[count];
            else
                chars = new char[count];

            int j = 0;

            // actually populate the array
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                // if we are not in  an escape sequence and the char is a escape char
                if (isEscaped == false && c == '\\')
                {
                    // escape
                    isEscaped = true;
                }
                // else if we are escaped
                else if (isEscaped)
                {
                    // reset escape state
                    isEscaped = false;

                    // check the char against the set of known escape chars
                    switch (char.ToLower(str[i]))
                    {
                        case '0':
                            chars[j++] = '\0';
                            break;

                        case 'a':
                            chars[j++] = '\a';
                            break;

                        case 'b':
                            chars[j++] = '\b';
                            break;

                        case 'f':
                            chars[j++] = '\f';
                            break;

                        case 'n':
                            chars[j++] = '\n';
                            break;

                        case 'r':
                            chars[j++] = '\r';
                            break;

                        case 't':
                            chars[j++] = '\t';
                            break;

                        case 'v':
                            chars[j++] = '\v';
                            break;

                        case '"':
                            chars[j++] = '"';
                            break;

                        case '\\':
                            chars[j++] = '\\';
                            break;

                        case 'x':
                            chars[j++] = (char) ((FromHex(str[++i]) << 4) | FromHex(str[++i]));
                            break;
                    }
                }
                else
                {
                    // normal char
                    chars[j++] = c;
                }
            }

            return chars.ToString();//new string(chars);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ByteFromHex(ReadOnlySpan<char> hex)
        {
            if (hex.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(hex));

            return unchecked((byte)((FromHex(hex[0]) << 4) + FromHex(hex[1])));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsHexChar(
            char digit) => (digit >= '0' && digit <= '9') ||
                           (digit >= 'a' && digit <= 'f') ||
                           (digit >= 'A' && digit <= 'F');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FromHex(char digit)
        {
            if (digit >= '0' && digit <= '9')
            {
                return digit - '0'; 
            }

            if (digit >= 'a' && digit <= 'f')
            {
                return digit - 'a' + 10;
            }
            
            if (digit >= 'A' && digit <= 'F')
            {
                return digit - 'A' + 10;
            }
            
            throw new ArgumentException("digit is not a valid hexadecimal digit (0-9, a-f, A-F).", nameof(digit));
        }

        /// <summary>
        /// Compute the number of bytes encoded in the specified Base 64 char array:
        /// Walk the entire input counting white spaces and padding chars, then compute result length
        /// based on 3 bytes per 4 chars.
        /// 
        /// The input must not have any extraneous whitespace.
        /// </summary>
        /// <remarks>Ported from src/libraries/System.Private.CoreLib/src/System/Convert.cs</remarks>
        internal static int FromBase64_ComputeResultLength(ReadOnlySpan<char> input)
        {
            const uint intEq = (uint)'=';
            const uint intSpace = (uint)' ';

            Debug.Assert(0 <= input.Length);

            //char* inputEndPtr = inputPtr + inputLength;
            int usefulInputLength = input.Length;
            int padding = 0;

            var inputUints = MemoryMarshal.Cast<char, uint>(input);

            foreach (uint c in inputUints)
            //while (inputPtr < inputEndPtr)
            {
                /*uint c = (uint)(*inputPtr);
                inputPtr++;*/

                // We want to be as fast as possible and filter out spaces with as few comparisons as possible.
                // We end up accepting a number of illegal chars as legal white-space chars.
                // This is ok: as soon as we hit them during actual decode we will recognise them as illegal and throw.
                if (c <= intSpace)
                    usefulInputLength--;
                else if (c == intEq)
                {
                    usefulInputLength--;
                    padding++;
                }
            }

            Debug.Assert(0 <= usefulInputLength);

            // For legal input, we can assume that 0 <= padding < 3. But it may be more for illegal input.
            // We will notice it at decode when we see a '=' at the wrong place.
            Debug.Assert(0 <= padding);

            // Perf: reuse the variable that stored the number of '=' to store the number of bytes encoded by the
            // last group that contains the '=':
            if (padding != 0)
            {
                if (padding == 1)
                    padding = 2;
                else if (padding == 2)
                    padding = 1;
                else
                    throw new FormatException("Bad base64 character!");
            }

            // Done:
            return (usefulInputLength / 4) * 3 + padding;
        }
    }
}