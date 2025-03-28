// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace OscCore.LowLevel
{
    public ref struct OscTypeTag(ReadOnlySpan<char> typeTag)
    {
        private readonly ReadOnlySpan<char> typeTag = typeTag;

        public readonly OscToken CurrentToken => GetTokenFromTypeTag(Index);

        public int Index { get; private set; } = 0;

        public OscToken NextToken()
        {
            return GetTokenFromTypeTag(++Index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetArgumentCount(out OscToken arrayType)
        {
            return GetArrayLength(0, out arrayType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetArrayElementCount(out OscToken arrayType)
        {
            return GetArrayLength(Index + 1, out arrayType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly OscToken GetTokenFromTypeTag(int index)
        {
            if (index == typeTag.Length)
                return OscToken.End;

            if (index < 0 || index > typeTag.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is not a valid part of the type tag");

            char type = typeTag[index];

            // ReSharper disable once SwitchStatementMissingSomeCases
            return type switch
            {
                'b' => OscToken.Blob,
                's' => OscToken.String,
                'S' => OscToken.Symbol,
                'i' => OscToken.Int,
                'h' => OscToken.Long,
                'f' => OscToken.Float,
                'd' => OscToken.Double,
                't' => OscToken.TimeTag,
                'c' => OscToken.Char,
                'r' => OscToken.Color,
                'm' => OscToken.Midi,
                'T' => OscToken.True,
                'F' => OscToken.False,
                'N' => OscToken.Null,
                'I' => OscToken.Impulse,
                '[' => OscToken.ArrayStart,
                ']' => OscToken.ArrayEnd,
                _ => throw new OscException(OscError.UnknownArgumentType, $@"Unknown OSC type '{type}' on argument '{index}'"),// Unknown argument type
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetArrayLength(int index, out OscToken arrayType)
        {
            arrayType = OscToken.None;

            if (index == typeTag.Length)
                return 0;

            if (index < 0 || index > typeTag.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is not a valid part of the type tag");

            int count = 0;
            int inset = 0;

            while (true)
            {
                OscToken token = GetTokenFromTypeTag(index++);

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (token)
                {
                    case OscToken.None:
                    case OscToken.OscAddress:
                    case OscToken.TypeTag:
                        throw new OscException(OscError.UnexpectedToken, $"Unexpected token {token}");
                    case OscToken.True:
                    case OscToken.False:
                        if (arrayType == OscToken.None)
                            arrayType = OscToken.Bool;
                        else if (arrayType != OscToken.Bool)
                            arrayType = OscToken.MixedTypes;

                        if (inset == 0)
                            count++;

                        break;
                    case OscToken.Null:
                        if (arrayType != OscToken.String &&
                            arrayType != OscToken.Blob)
                            arrayType = OscToken.MixedTypes;

                        if (inset == 0)
                            count++;

                        break;
                    case OscToken.String:
                    case OscToken.Blob:
                    case OscToken.Char:
                    case OscToken.Symbol:
                    case OscToken.Impulse:
                    case OscToken.Int:
                    case OscToken.Long:
                    case OscToken.Float:
                    case OscToken.Double:
                    case OscToken.TimeTag:
                    case OscToken.Color:
                    case OscToken.Midi:
                        if (arrayType == OscToken.None)
                            arrayType = token;
                        else if (arrayType != token)
                            arrayType = OscToken.MixedTypes;

                        if (inset == 0)
                            count++;

                        break;
                    case OscToken.ArrayStart:
                        if (inset == 0)
                            count++;

                        inset++;
                        break;
                    case OscToken.ArrayEnd:
                        inset--;

                        if (inset == -1)
                            return count;

                        break;
                    case OscToken.End:
                        return count;
                    case OscToken.MixedTypes:
                    default:
                        throw new OscException(OscError.UnknownArgumentType, $@"Unknown OSC type '{token}' on argument '{index}'");
                }
            }
        }
    }
}