﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenZH.Data.Utilities.Extensions
{
    internal static class BinaryReaderExtensions
    {
        public static uint ReadBigEndianUInt32(this BinaryReader reader)
        {
            var array = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return BitConverter.ToUInt32(array, 0);
        }

        public static uint ReadUInt24(this BinaryReader reader)
        {
            var result = 0u;
            for (var i = 0; i < 3; i++)
            {
                result |= ((uint) reader.ReadByte() << (i * 8));
            }
            return result;
        }

        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            var sb = new StringBuilder();

            char c;
            while ((c = reader.ReadChar()) != '\0')
            {
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static string ReadUInt16PrefixedAsciiString(this BinaryReader reader)
        {
            var length = reader.ReadUInt16();
            return Encoding.ASCII.GetString(reader.ReadBytes(length));
        }

        public static string ReadUInt16PrefixedUnicodeString(this BinaryReader reader)
        {
            var length = reader.ReadUInt16();
            return Encoding.Unicode.GetString(reader.ReadBytes(length * 2));
        }

        public static T ReadStruct<T>(this BinaryReader reader)
            where T : struct
        {
            var bytes = reader.ReadBytes(Marshal.SizeOf<T>());

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var theStructure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();

            return theStructure;
        }

        public static string ReadFixedLengthString(this BinaryReader reader, int count)
        {
            var chars = reader.ReadChars(count);
            return new string(chars).TrimEnd('\0');
        }

        public static ushort[,] ReadUInt16Array2D(this BinaryReader reader, uint width, uint height)
        {
            var result = new ushort[width, height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    result[x, y] = reader.ReadUInt16();
                }
            }

            return result;
        }

        public static bool[,] ReadSingleBitBooleanArray2D(this BinaryReader reader, uint width, uint height)
        {
            var result = new bool[width, height];

            for (var y = 0; y < height; y++)
            {
                var temp = (byte) 0;
                for (var x = 0; x < width; x++)
                {
                    if (x % 8 == 0)
                    {
                        temp = reader.ReadByte();
                    }
                    result[x, y] = (temp & (1 << (x % 8))) > 0;
                }
            }

            return result;
        }

        public static TEnum ReadUInt32AsEnum<TEnum>(this BinaryReader reader)
            where TEnum : struct
        {
            var value = reader.ReadUInt32();

            return CastValueAsEnum<uint, TEnum>(value);
        }

        public static TEnum ReadInt32AsEnum<TEnum>(this BinaryReader reader)
           where TEnum : struct
        {
            var value = reader.ReadInt32();

            return CastValueAsEnum<int, TEnum>(value);
        }

        public static TEnum ReadByteAsEnum<TEnum>(this BinaryReader reader)
           where TEnum : struct
        {
            var value = reader.ReadByte();

            return CastValueAsEnum<byte, TEnum>(value);
        }

        private static TEnum CastValueAsEnum<TValue, TEnum>(TValue value)
           where TEnum : struct
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new InvalidDataException($"Unexpected value for {typeof(TEnum).Name}: {value}");
            }

            return (TEnum) (object) value;
        }
    }
}
