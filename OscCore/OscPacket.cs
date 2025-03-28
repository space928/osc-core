// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;
using System.Runtime.InteropServices;
using OscCore.LowLevel;

namespace OscCore
{
    /// <summary>
    /// Discriminated union type for OSC messages and bundles.
    /// </summary>
    //[StructLayout(LayoutKind.Explicit)]
    public readonly struct OscPacket
    {
        /*[FieldOffset(0)]*/ private readonly OscPacketKind kind;
        /*[FieldOffset(4)]*/ private readonly OscMessage oscMessage;
        /*[FieldOffset(4)]*/ private readonly OscBundle oscBundle;

        public OscPacketKind Kind => kind;
        public OscMessage OscMessage => kind == OscPacketKind.OscMessage ? oscMessage : throw new FieldAccessException();
        public OscBundle OscBundle => kind == OscPacketKind.OscBundle ? oscBundle : throw new FieldAccessException();

        public int SizeInBytes
        {
            get
            {
                return kind switch
                {
                    OscPacketKind.OscMessage => oscMessage.SizeInBytes,
                    OscPacketKind.OscBundle => oscBundle.SizeInBytes,
                    _ => throw new NotImplementedException(),
                };
            }
        }

        public OscPacket(in OscMessage msg)
        {
            this.kind = OscPacketKind.OscMessage;
            this.oscMessage = msg;
        }

        public OscPacket(in OscBundle bundle)
        {
            this.kind = OscPacketKind.OscBundle;
            this.oscBundle = bundle;
        }

        public static implicit operator OscPacket(in OscMessage msg) => new(in msg);
        public static implicit operator OscPacket(in OscBundle bundle) => new(in bundle);

        public static OscPacket Parse(ReadOnlySpan<char> str, IFormatProvider? provider = null)
        {
            if (str.IsWhiteSpace())
                throw new ArgumentNullException(nameof(str));

            OscStringReader reader = new(str);

            return Parse(ref reader, provider, OscSerializationToken.End);
        }

        public static OscPacket Parse(ref OscStringReader reader, IFormatProvider? provider = null, OscSerializationToken endToken = OscSerializationToken.End)
        {
            if (reader.PeekChar() == '#')
                return new(OscBundle.Parse(ref reader, provider, endToken));

            return new(OscMessage.Parse(ref reader, provider, endToken));
        }

        /// <summary>
        ///     Read the osc packet from a byte array
        /// </summary>
        /// <param name="bytes">array to read from</param>
        /// <param name="index">the offset within the array where reading should begin</param>
        /// <param name="count">the number of bytes in the packet</param>
        /// <param name="origin">the origin that is the origin of this packet</param>
        /// <param name="timeTag">the time tag asociated with the parent</param>
        /// <returns>the packet</returns>
        public static OscPacket Read(byte[] bytes, int index, int count, Uri? origin = null, OscTimeTag? timeTag = null)
        {
            if (bytes[index] == (byte)'#')
                return new(OscBundle.Read(bytes, index, count, origin));

            return new(OscMessage.Read(bytes, index, count, origin, timeTag));
        }

        public static OscPacket Read(ref OscReader reader, int count, Uri? origin = null, OscTimeTag? timeTag = null)
        {
            if (reader.PeekByte() == (byte)'#')
                return new(OscBundle.Read(ref reader, count, origin));

            return new(OscMessage.Read(ref reader, count, origin, timeTag));
        }

        /// <summary>
        ///     Get an array of bytes containing the entire packet
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            return kind switch
            {
                OscPacketKind.OscMessage => oscMessage.ToByteArray(),
                OscPacketKind.OscBundle => oscBundle.ToByteArray(),
                _ => throw new NotImplementedException(),
            };
        }

        public void Write(ref OscWriter writer)
        {
            switch (kind)
            {
                case OscPacketKind.OscMessage: oscMessage.Write(ref writer); break;
                case OscPacketKind.OscBundle: oscBundle.Write(ref writer); break;
                default: throw new NotImplementedException();
            };
        }

        public void WriteToString(OscStringWriter writer)
        {
            switch (kind)
            {
                case OscPacketKind.OscMessage: oscMessage.WriteToString(writer); break;
                case OscPacketKind.OscBundle: oscBundle.WriteToString(writer); break;
                default: throw new NotImplementedException();
            };
        }

        // TODO: Either invert the parse logic to avoid having to try catch the whole read, or remove this API method.
        public static bool TryParse(string str, out OscPacket packet)
        {
            try
            {
                packet = Parse(str);
                return true;
            }
            catch
            {
                packet = default;
                return false;
            }
        }

        public static bool TryParse(string str, IFormatProvider provider, out OscPacket packet)
        {
            try
            {
                packet = Parse(str, provider);
                return true;
            }
            catch
            {
                packet = default;
                return false;
            }
        }

    }

    public enum OscPacketKind : int
    {
        OscMessage,
        OscBundle,
    }
}