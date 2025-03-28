// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using OscCore.Address;

namespace OscCore.LowLevel
{
    public ref struct OscStringReader
    {
        private readonly ReadOnlySpan<char> original;
        private int position;
        private readonly int maxPosition;

        public OscStringReader(ReadOnlySpan<char> value)
        {
            original = value;
            position = 0;
            maxPosition = value.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAddress(bool validate)
        {
            OscSerializationToken token = ReadNextToken(out var value);

            if (token != OscSerializationToken.Literal)
                throw new OscException(OscError.ErrorParsingOscAdress, $"Unexpected serialization token {token}");

            string address = value.Trim().ToString();

            if (validate != true)
                return address;

            if (string.IsNullOrWhiteSpace(address))
                throw new Exception("Address was empty");

            if (OscAddress.IsValidAddressPattern(address) == false)
                throw new Exception("Invalid address");

            return address;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadSeparator()
        {
            OscSerializationToken token = ReadNextToken(out _);

            if (token != OscSerializationToken.Separator)
                throw new OscException(OscError.ErrorParsingOscAdress, $"Unexpected serialization token {token}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OscSerializationToken ReadSeparatorOrEnd()
        {
            OscSerializationToken token = ReadNextToken(out _);

            if (token != OscSerializationToken.Separator &&
                token != OscSerializationToken.ArrayEnd &&
                token != OscSerializationToken.ObjectEnd &&
                token != OscSerializationToken.End)
            {
                throw new OscException(OscError.ErrorParsingOscAdress, $"Unexpected serialization token {token}");
            }

            return token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object[] ReadArguments(IFormatProvider? provider, OscSerializationToken endToken)
        {
            List<object> arguments = [];

            OscSerializationToken token;

            do
            {
                token = ReadNextToken(out var value);

                if (token == endToken)
                    break;

                switch (token)
                {
                    case OscSerializationToken.Literal:
                        arguments.Add(ParseLiteral(value, provider));
                        break;
                    case OscSerializationToken.String:
                        arguments.Add(ParseString(value, provider));
                        break;
                    case OscSerializationToken.Symbol:
                        arguments.Add(ParseSymbol(value, provider));
                        break;
                    case OscSerializationToken.Char:
                        arguments.Add(ParseChar(value, provider));
                        break;
                    case OscSerializationToken.Separator:
                        break;
                    case OscSerializationToken.ArrayStart:
                        arguments.Add(ReadArray(provider));
                        break;
                    case OscSerializationToken.ObjectStart:
                        arguments.Add(ParseObject(value, provider));
                        break;
                    case OscSerializationToken.End:
                        break;
                    case OscSerializationToken.None:
                    case OscSerializationToken.ObjectEnd:
                    case OscSerializationToken.ArrayEnd:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            while (token != endToken && token != OscSerializationToken.End);

            if (token != endToken)
                throw new OscException(OscError.UnexpectedToken, $"Unexpected token {token}");

            return arguments.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object[] ReadArray(IFormatProvider? provider)
        {
            OscSerializationToken endToken = OscSerializationToken.ArrayEnd;

            List<object> arguments = [];

            OscSerializationToken token;

            do
            {
                token = ReadNextToken(out var value);

                switch (token)
                {
                    case OscSerializationToken.Literal:
                        arguments.Add(ParseLiteral(value, provider));
                        break;
                    case OscSerializationToken.String:
                        arguments.Add(ParseString(value, provider));
                        break;
                    case OscSerializationToken.Symbol:
                        arguments.Add(ParseSymbol(value, provider));
                        break;
                    case OscSerializationToken.Char:
                        arguments.Add(ParseChar(value, provider));
                        break;
                    case OscSerializationToken.Separator:
                        break;
                    case OscSerializationToken.ArrayStart:
                        arguments.Add(ReadArray(provider));
                        break;
                    case OscSerializationToken.ArrayEnd:
                        break;
                    case OscSerializationToken.ObjectStart:
                        arguments.Add(ParseObject(value, provider));
                        break;
                    case OscSerializationToken.None:
                    case OscSerializationToken.End:
                    case OscSerializationToken.ObjectEnd:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            while (token != endToken && token != OscSerializationToken.End);

            if (token != endToken)
            {
                throw new OscException(OscError.UnexpectedToken, $"Unexpected token {token}");
            }

            return arguments.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly object ParseSymbol(ReadOnlySpan<char> value, IFormatProvider? provider)
        {
            return new OscSymbol(OscSerializationUtils.Unescape(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ParseObject(ReadOnlySpan<char> value, IFormatProvider? provider)
        {
            var name = ReadObjectNameToken();

            if (name.Length == 0)
                throw new Exception(@"Malformed object missing type name");

            // The length here is the maximum expected type length
            if (name.Length > 5)
                throw new Exception($@"Unknown object type '{name.ToString()}'");
            Span<char> nameLower = stackalloc char[name.Length];
            name.ToLowerInvariant(nameLower);

            return nameLower switch
            {
                "midi" or "m" => OscMidiMessage.Parse(ref this, provider),
                "time" or "t" => OscTimeTag.Parse(ref this, provider),
                "color" or "c" => OscColor.Parse(ref this, provider),
                "blob" or "b" or "data" or "d" => ParseBlob(provider),
                _ => throw new Exception($@"Unknown object type '{name.ToString()}'"),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] ParseBlob(IFormatProvider? provider)
        {
            OscSerializationToken token = ReadNextToken(out var value);

            if (token == OscSerializationToken.ObjectEnd)
                return [];

            if (token == OscSerializationToken.Literal)
            {
                var trimmed = value.Trim();

                if (trimmed.StartsWith("64x"))
                {
                    trimmed = trimmed[3..].TrimEnd();
                    var expectedLength = OscSerializationUtils.FromBase64_ComputeResultLength(trimmed);
                    byte[] bytes = new byte[expectedLength];
                    Convert.TryFromBase64Chars(trimmed, bytes, out _);

                    if (ReadNextToken(out _) != OscSerializationToken.ObjectEnd)
                        throw new Exception("Expected end of object");

                    return bytes;
                }

                if (trimmed.StartsWith("0x"))
                {
                    trimmed = trimmed[2..];

                    if (trimmed.Length % 2 != 0)
                    {
                        // this is an error
                        throw new Exception("Invalid blob string length");
                    }

                    int length = trimmed.Length / 2;

                    byte[] bytes = new byte[length];

                    for (int i = 0; i < bytes.Length; i++)
                        bytes[i] = OscSerializationUtils.ByteFromHex(trimmed.Slice(i * 2, 2));

                    if (ReadNextToken(out _) != OscSerializationToken.ObjectEnd)
                        throw new Exception("Expected end of object");

                    return bytes;
                }
            }

            int pos = 0;
            byte[] buff;
            buff = ArrayPool<byte>.Shared.Rent(64);

            while (token != OscSerializationToken.ObjectEnd)
            {
                if (pos + 1 >= buff.Length)
                {
                    var nbuff = ArrayPool<byte>.Shared.Rent(buff.Length << 1);
                    Array.Copy(buff, nbuff, buff.Length);
                    ArrayPool<byte>.Shared.Return(buff);
                    buff = nbuff;
                }

                buff[pos++] = byte.Parse(value.Trim());

                token = ReadNextToken(out value);

                if (token == OscSerializationToken.Separator)
                    token = ReadNextToken(out value);
            }

            byte[] returnBuff = new byte[pos];
            Array.Copy(buff, returnBuff, returnBuff.Length);
            ArrayPool<byte>.Shared.Return(buff);

            return returnBuff;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly object ParseChar(ReadOnlySpan<char> value, IFormatProvider? provider)
        {
            // TODO: Avoid the pointless alloc
            string unescapeString = OscSerializationUtils.Unescape(value);

            if (unescapeString.Length > 1)
                throw new Exception();

            char c = unescapeString.AsSpan().Trim()[0];

            return (byte)c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly object ParseString(ReadOnlySpan<char> value, IFormatProvider? provider)
        {
            return OscSerializationUtils.Unescape(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly object ParseLiteral(ReadOnlySpan<char> value, IFormatProvider? provider)
        {
            long valueInt64;
            float valueFloat;
            double valueDouble;

            var argString = value.Trim();

            if (argString.Length == 0)
                throw new Exception("Argument is empty");

            // try to parse a hex value
            if (argString.Length > 2 && argString.StartsWith("0x"))
            {
                var hexString = argString[2..];

                // parse a int32
                if (hexString.Length <= 8)
                {
                    if (uint.TryParse(hexString, NumberStyles.HexNumber, provider, out uint valueUInt32))
                    {
                        return unchecked((int)valueUInt32);
                    }
                }
                // parse a int64
                else
                {
                    if (ulong.TryParse(hexString, NumberStyles.HexNumber, provider, out ulong valueUInt64))
                        return unchecked((long)valueUInt64);
                }
            }

            // parse int64
            if (argString[^1] == 'L')
                if (long.TryParse(argString[..^1], NumberStyles.Integer, provider, out valueInt64))
                    return valueInt64;

            // parse int32
            if (int.TryParse(argString, NumberStyles.Integer, provider, out int valueInt32))
                return valueInt32;

            // parse int64
            if (long.TryParse(argString, NumberStyles.Integer, provider, out valueInt64))
                return valueInt64;

            // parse double
            if (argString.EndsWith("d"))
                if (double.TryParse(argString[..^1], NumberStyles.Float, provider, out valueDouble))
                    return valueDouble;

            // parse float
            if (argString.EndsWith("f"))
                if (float.TryParse(argString[..^1], NumberStyles.Float, provider, out valueFloat))
                    return valueFloat;

            // TODO: These strings are basically always const...
            /*if (argString.SequenceEqual(float.PositiveInfinity.ToString(provider)))
                return float.PositiveInfinity;

            if (argString.SequenceEqual(float.NegativeInfinity.ToString(provider)))
                return float.NegativeInfinity;

            if (argString.SequenceEqual(float.NaN.ToString(provider)))
                return float.NaN;*/

            // parse float 
            if (float.TryParse(argString, NumberStyles.Float, provider, out valueFloat))
                return valueFloat;

            // parse double
            if (double.TryParse(argString, NumberStyles.Float, provider, out valueDouble))
                return valueDouble;

            // parse bool
            if (bool.TryParse(argString, out bool valueBool))
                return valueBool;

            // parse null 
            if (OscNull.IsNull(argString))
                return OscNull.Value;

            // parse impulse/bang
            if (OscImpulse.IsImpulse(argString))
            {
                return OscImpulse.Value;
            }

            // if all else fails then its a symbol i guess (?!?) 
            return new OscSymbol(argString.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipWhiteSpace()
        {
            for (; position < maxPosition; position++)
            {
                char @char = original[position];

                switch (@char)
                {
                    case ' ':
                    case '\n':
                    case '\r':
                    case '\t':
                        continue;
                    default:
                        return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OscSerializationToken ReadNextToken(out ReadOnlySpan<char> value)
        {
            SkipWhiteSpace();

            value = null;

            if (position >= maxPosition)
                return OscSerializationToken.End;

            char @char = original[position];

            switch (@char)
            {
                case '$':
                    position++;

                    if (original[position] == '"')
                    {
                        position++;
                        value = ReadStringToken();
                        return OscSerializationToken.Symbol;
                    }
                    else
                    {
                        position--;
                        value = ReadLiteralToken();
                        return OscSerializationToken.Literal;
                    }

                case '"':
                    position++;
                    value = ReadStringToken();
                    return OscSerializationToken.String;

                case '\'':
                    position++;
                    value = ReadCharToken();
                    return OscSerializationToken.Char;

                case ',':
                    position++;
                    return OscSerializationToken.Separator;

                case '[':
                    position++;
                    return OscSerializationToken.ArrayStart;

                case ']':
                    position++;
                    return OscSerializationToken.ArrayEnd;

                case '{':
                    position++;
                    return OscSerializationToken.ObjectStart;

                case '}':
                    position++;
                    return OscSerializationToken.ObjectEnd;

                default:
                    value = ReadLiteralToken();
                    return OscSerializationToken.Literal;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadLiteralToken()
        {
            int start = position;

            int index = original[position..].IndexOfAny(LiteralTokenControlChars) + position;

            if (index < start || index > maxPosition)
                index = maxPosition;

            position = index;

            return original[start..position];
        }

        private static readonly char[] ObjectNameControlChars = [ ',', ':', ']', '}' ];
        private static readonly char[] LiteralTokenControlChars = [ ',', ']', '}' ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadObjectNameToken()
        {
            int start = position;
            bool valid = true;
            int index = original[position..].IndexOfAny(ObjectNameControlChars) + start;

            if (index < start || index > maxPosition)
                index = maxPosition;
            else
                valid = original[index] == ':';

            var value = original[start..index].Trim();

            if (valid == false)
                throw new OscException(OscError.InvalidObjectName, $"Invalid object name {value.ToString()}");

            position = index + 1;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadCharToken()
        {
            bool escaped = false;

            int start = position;
            int end = start;

            for (; position < maxPosition; position++)
            {
                char @char = original[position];
                end = position;

                if (escaped)
                    continue;

                if (@char == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (@char == '\'')
                {
                    position++;
                    break;
                }
            }

            return original[start..end];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadStringToken()
        {
            bool escaped = false;

            int start = position;
            int end = start;

            for (; position < maxPosition; position++)
            {
                char @char = original[position];
                end = position;

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (@char == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (@char == '"')
                {
                    position++;
                    break;
                }
            }

            return original[start..end];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char PeekChar()
        {
            int startPosition = position;
            SkipWhiteSpace();
            char value = original[position];
            position = startPosition;

            return value;
        }
    }
}