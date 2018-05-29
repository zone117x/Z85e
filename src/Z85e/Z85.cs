﻿using System;
using CoenM.Encoding.Internals.Guards;

namespace CoenM.Encoding
{
    using Internals;

    /// <summary>
    /// Z85 Encoding library
    /// </summary>
    /// <remarks>This implementation is heavily based on https://github.com/zeromq/rfc/blob/master/src/spec_32.c </remarks>
    public static class Z85
    {
        /// <summary>Calculate output size after decoding the z85 characters.</summary>
        /// <param name="source">encoded string</param>
        /// <returns>size of output after decoding</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length of <paramref name="source"/> is not a multiple of 5.</exception>
        public static int CalcuateDecodedSize(ReadOnlySpan<char> source)
        {
            Guard.MustHaveSizeMultipleOf(source, 5, nameof(source));

            return source.Length / 5 * 4;
        }

        /// <summary>Calculate string size after encoding bytes using the Z85 encoder.</summary>
        /// <param name="source">bytes to encode</param>
        /// <returns>size of the encoded string</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length of <paramref name="source"/> is not a multiple of 4.</exception>
        public static int CalcuateEncodedSize(ReadOnlySpan<byte> source)
        {
            Guard.MustHaveSizeMultipleOf(source, 4, nameof(source));

            return source.Length / 4 * 5;
        }

        /// <summary>Decode an encoded string (<paramref name="source"/>) to bytes (<paramref name="destination"/>).</summary>
        /// <remarks>This method will not check if <paramref name="source"/> only exists of Z85 characters.</remarks>
        /// <param name="source">encoded string. Should have length multiple of 5.</param>
        /// <param name="destination">placeholder for the decoded result. Should have sufficient length.</param>
        /// <returns>number of bytes written to <paramref name="destination"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length of <paramref name="source"/> is not a multiple of 5, or when destination doesn't have sufficient space.</exception>
        public static int Decode(ReadOnlySpan<char> source, Span<byte> destination)
        {
            Guard.MustHaveSizeMultipleOf(source, 5, nameof(source));

            var decodedSize = CalcuateDecodedSize(source);
            Guard.MustBeSizedAtLeast(destination, decodedSize, nameof(destination));

            var len = source.Length;
            var byteNbr = 0;
            var charNbr = 0;
            uint value = 0;

            while (charNbr < len)
            {
                //  Accumulate value in base 85
                value = value * 85 + Map.Decoder[(byte)source[charNbr++] - 32];
                if (charNbr % 5 != 0)
                    continue;

                //  Output value in base 256
                var divisor = 256 * 256 * 256;
                while (divisor != 0)
                {
                    destination[byteNbr++] = (byte)(value / divisor % 256);
                    divisor /= 256;
                }
                value = 0;
            }

            return decodedSize;
        }


        /// <summary>Decode an encoded string into a byte array. Output size will be length of <paramref name="input"/> * 4 / 5.</summary>
        /// <remarks>This method will not check if <paramref name="input"/> only exists of Z85 characters.</remarks>
        /// <param name="input">encoded string. Should have length multiple of 5.</param>
        /// <returns>empty bytes when <paramref name="input"/> is null, otherwise bytes containing the decoded input string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length of <paramref name="input"/> is not a multiple of 5.</exception>
        public static ReadOnlySpan<byte> Decode(string input)
        {
            Guard.NotNull(input, nameof(input));

            var inputSpan = input.AsSpan();

            var decodedSize = CalcuateDecodedSize(inputSpan);

            Span<byte> decoded = decodedSize <= 128
                ? stackalloc byte[decodedSize]
                : new byte[decodedSize];


            var len = Decode(inputSpan, decoded);

            // todo: is this the way to do this?
            // maybe better to return a Memory<T>
            return decoded.Slice(0, len).ToArray();
        }


        /// <summary>Encode bytes (<paramref name="source"/>) to characters (<paramref name="destination"/>).</summary>
        /// <param name="source">bytes to encode. Length should be multiple of 4.</param>
        /// <param name="destination">placeholder for the ecoded result. Should have sufficient length.</param>
        /// <returns>number of characters written to <paramref name="destination"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length of <paramref name="source"/> is not a multiple of 4, or when destination doesn't have sufficient space.</exception>
        public static int Encode(ReadOnlySpan<byte> source, Span<char> destination)
        {
            Guard.MustHaveSizeMultipleOf(source, 4, nameof(source));

            var encodedSize = CalcuateEncodedSize(source);
            Guard.MustBeSizedAtLeast(destination, encodedSize, nameof(destination));

            uint charNbr = 0;
            uint byteNbr = 0;
            uint value = 0;

            while (byteNbr < source.Length)
            {
                //  Accumulate value in base 256 (binary)
                value = value * 256 + source[(int)byteNbr++];
                if (byteNbr % 4 != 0)
                    continue;

                //  Output value in base 85
                uint divisor = 85 * 85 * 85 * 85;
                while (divisor != 0)
                {
                    destination[(int)charNbr++] = Map.Encoder[value / divisor % 85];
                    divisor /= 85;
                }
                value = 0;
            }

            return encodedSize;
        }

        /// <summary>Encode bytes as a string. Output size will be length of <paramref name="source"/> / 4 * 5. </summary>
        /// <param name="source">bytes to encode. Length should be multiple of 4.</param>
        /// <returns>Encoded string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length of <paramref name="source"/> is not a multiple of 4.</exception>
        public static string Encode(ReadOnlySpan<byte> source)
        {
            var encodedSize = CalcuateEncodedSize(source);

            Span<char> encoded = encodedSize <= 128
                ? stackalloc char[encodedSize]
                : new char[encodedSize];

            var len = Encode(source, encoded);

            // todo: is this the way to do this?
            // maybe better to return a Memory<T>
            return new string(encoded.Slice(0, len).ToArray());
        }
    }
}