﻿// Copyright (c) Tilde Love Project. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

using System;
using OscCore;
using Xunit;

namespace OscCoreTests
{
    public class OscBundleRawTest
    {
        /// <summary>
        ///A test for Write
        ///</summary>
        [Fact]
        public void Nested_ReadTest()
        {
            byte[] bytes = UnitTestHelper.DoubleNestedBundleBody;
            OscBundleRaw actual = new OscBundleRaw(new ArraySegment<byte>(bytes, 0, bytes.Length));

            Assert.Equal(2, actual.Count);
            
            for (int i = 0; i < actual.Count; i++)
            {
                OscMessageRaw raw = actual[i];

                Assert.Equal("/aa", raw.Address);

                OscArgument argument = raw[0];

                int value = raw.ReadInt(ref argument);

                Assert.Equal(-1, value);
            }
        }
       
        [Fact]
        public void OscBundleManyMessagesTest_1()
        {            
            OscBundle expected = new OscBundle(new OscTimeTag(0),
                new OscMessage("/ping"), new OscMessage("/moop"), new OscMessage("/ping"), new OscMessage("/ping"), new OscMessage("/ping"));

            byte[] bytes = expected.ToByteArray(); 
            
            OscBundleRaw actual = new OscBundleRaw(new ArraySegment<byte>(bytes, 0, bytes.Length));

            Assert.Equal(actual.Count, expected.Count);
            
            for (int i = 0; i < actual.Count; i++)
            {
                OscMessageRaw raw = actual[i];
                Assert.Equal(OscPacketKind.OscMessage, expected[i].Kind);
                OscMessage expectedMessage = expected[i].OscMessage; 
                
                Assert.Equal(raw.Address, expectedMessage.Address);

            }
        }

        /// <summary>
        ///A test for Read
        ///</summary>
        [Fact]
        public void ReadTest_Bad_ToLong()
        {
            try
            {
                byte[] bytes =
                {
                    // #bundle
                    35, 98, 117, 110, 100, 108, 101, 0,
                    
                    // Time-tag
                    197, 146, 134, 227, 3, 18, 110, 152,

                    // length
                    0, 0, 0, 64, // 32,

                    // message body
                    47, 116, 101, 115, 116, 0, 0, 0,
                    44, 105, 91, 105, 105, 105, 93, 0,

                    26, 42, 58, 74, 26, 42, 58, 74,
                    90, 106, 122, 138, 154, 170, 186, 202
                };
                
                OscBundleRaw actual = new OscBundleRaw(new ArraySegment<byte>(bytes, 0, bytes.Length));

                Assert.Fail("Exception not thrown");
            }
            catch (OscException ex)
            {
                Assert.Equal(OscError.InvalidBundleMessageLength, ex.OscError);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        /// <summary>
        ///A test for Read
        ///</summary>
        [Fact]
        public void ReadTest_Bad_ToShort()
        {
            try
            {
                byte[] bytes =
                {
                    // #bundle
                    35, 98, 117, 110, 100, 108, 101, 0,

                    // Time-tag
                    197, 146, 134, 227, 3, 18, 110, 152,

                    // length
                    0, 0, 0, 24, // 32,

                    // message body
                    47, 116, 101, 115, 116, 0, 0, 0,
                    44, 105, 91, 105, 105, 105, 93, 0,

                    26, 42, 58, 74, 26, 42, 58, 74,
                    90, 106, 122, 138, 154, 170, 186, 202
                };

                OscBundleRaw actual = new OscBundleRaw(new ArraySegment<byte>(bytes, 0, bytes.Length));

                Assert.Fail("Exception not thrown");
            }
            catch (OscException ex)
            {
                Assert.Equal(OscError.UnexpectedToken, ex.OscError);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}