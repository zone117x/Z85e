namespace CoenM.Encoding
{
    using System;

    using CoenM.Encoding.Internals;

#if !FEATURE_NULLABLE
#nullable disable
#else
    using System.Diagnostics.CodeAnalysis;

    using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;
#endif

    /// <summary>
    /// Z85 Extended Encoding library. Z85 Extended doesn't require the length of the bytes to be a multiple of 4.
    /// </summary>
    public static class Z85Extended
    {
        // Get pointers to avoid unnecessary range checking
        private static readonly ReadOnlyMemory<char> Z85EncoderMap = Map.Encoder;

        // Get a pointers to avoid unnecessary range checking
        private static readonly ReadOnlyMemory<byte> Z85DecoderMap = Map.Decoder;

        /// <summary>
        /// Decode an encoded string into a byte array. Output size will roughly be 'length of <paramref name="input"/>' * 4 / 5.
        /// </summary>
        /// <remarks>This method will not check if <paramref name="input"/> only exists of Z85 characters.</remarks>
        /// <param name="input">encoded string.</param>
        /// <returns><c>null</c> when <paramref name="input"/> is null, otherwise bytes containing the decoded input string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length of <paramref name="input"/> is a multiple of 5 plus 1.</exception>
#if FEATURE_NULLABLE
        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "C#8.0 feature")]
        [return: NotNullIfNotNull("input")]
        [return: MaybeNull]
        public static byte[]? Decode([DisallowNull] string input)
#else
        public static unsafe byte[] Decode([NotNull] string input)
#endif
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once HeuristicUnreachableCode
            if (input == null)
                return null;

            var inputLength = input.Length;
            var remainder = inputLength % 5;

            int decodedSize;
            int extraBytes;
            if (remainder == 0)
            {
                extraBytes = 0;
                decodedSize = inputLength * 4 / 5;
            }
            else
            {
                extraBytes = remainder - 1;
                decodedSize = ((inputLength - extraBytes) * 4 / 5) + extraBytes;
            }

            // two chars are decoded to one byte
            // thee chars to two bytes
            // four chars to three bytes.
            // therefore, remainder of one byte should not be possible.
            if (remainder == 1)
                throw new ArgumentException("Input length % 5 cannot be 1.");

            byte[] decoded = new byte[decodedSize];

            uint byteNbr = 0;
            int charNbr = 0;
            uint value;

            const uint divisor3 = 256 * 256 * 256;
            const uint divisor2 = 256 * 256;
            const uint divisor1 = 256;

            int len = inputLength - remainder;

            ReadOnlySpan<char> src = input;
            ReadOnlySpan<byte> z85Decoder = Z85DecoderMap.Span;

            while (charNbr < len)
            {
                // Accumulate value in base 85
                value = z85Decoder[(byte)src[charNbr]];
                value = (value * 85) + z85Decoder[(byte)src[charNbr + 1]];
                value = (value * 85) + z85Decoder[(byte)src[charNbr + 2]];
                value = (value * 85) + z85Decoder[(byte)src[charNbr + 3]];
                value = (value * 85) + z85Decoder[(byte)src[charNbr + 4]];
                charNbr += 5;

                // Output value in base 256
                decoded[byteNbr + 0] = (byte)((value / divisor3) % 256);
                decoded[byteNbr + 1] = (byte)((value / divisor2) % 256);
                decoded[byteNbr + 2] = (byte)((value / divisor1) % 256);
                decoded[byteNbr + 3] = (byte)(value % 256);
                byteNbr += 4;
            }

            if (extraBytes != 0)
            {
                value = 0;
                while (charNbr < inputLength)
                    value = (value * 85) + Map.Decoder[(byte)input[charNbr++]];

                // Take care of the remainder.
                var divisor = (uint)Math.Pow(256, extraBytes - 1);
                while (divisor != 0)
                {
                    decoded[byteNbr++] = (byte)((value / divisor) % 256);
                    divisor /= 256;
                }
            }

            return decoded;
        }

        /// <summary>
        /// Calculates the length of the encoded output string length.
        /// </summary>
        /// <param name="dataLength">Byte length of input data.</param>
        /// <returns>Length of the encoded output string.</returns>
        public static int GetEncodedSize(int dataLength)
        {
            var size = dataLength;
            var remainder = size % 4;
            if (remainder == 0)
            {
                return dataLength * 5 / 4;
            }

            // one byte -> two chars
            // two bytes -> three chars
            // three byte -> four chars
            var extraChars = remainder + 1;

            var encodedSize = ((size - remainder) * 5 / 4) + extraChars;
            return encodedSize;
        }

        /// <summary>
        /// Encode a byte array as a string. Output size will roughly be 'length of <paramref name="data"/>' / 4 * 5.
        /// </summary>
        /// <param name="data">byte[] to encode. No restrictions on the length.</param>
        /// <returns>Encoded string or <c>null</c> when the <paramref name="data"/> was null.</returns>
#if FEATURE_NULLABLE
        [return: NotNullIfNotNull("data")]
        [return: MaybeNull]
        public static string? Encode([DisallowNull] byte[] data)
#else
        public static unsafe string Encode([NotNull] byte[] data)
#endif
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once HeuristicUnreachableCode
            if (data == null)
                return null;

            var size = data.Length;
            var remainder = size % 4;
            int charNbr = 0;
            uint byteNbr = 0;
            Span<char> z85Dest;
            int len;
            if (remainder == 0)
            {
                len = size;
                z85Dest = new char[size * 5 / 4];
            }
            else
            {
                // one byte -> two chars
                // two bytes -> three chars
                // three byte -> four chars
                var extraChars = remainder + 1;

                var encodedSize = ((size - remainder) * 5 / 4) + extraChars;
                z85Dest = new char[encodedSize];
                var size2 = size - remainder;
                len = size2;
            }

            const uint divisor4 = 85 * 85 * 85 * 85;
            const uint divisor3 = 85 * 85 * 85;
            const uint divisor2 = 85 * 85;
            const uint divisor1 = 85;
            const int byte3 = 256 * 256 * 256;
            const int byte2 = 256 * 256;
            const int byte1 = 256;

            ReadOnlySpan<char> z85Encoder = Z85EncoderMap.Span;

            uint value;
            while (byteNbr < len)
            {
                // Accumulate value in base 256 (binary)
                value = (uint)((data[byteNbr + 0] * byte3) +
                                (data[byteNbr + 1] * byte2) +
                                (data[byteNbr + 2] * byte1) +
                                data[byteNbr + 3]);
                byteNbr += 4;

                // Output value in base 85
                z85Dest[charNbr + 0] = z85Encoder[(byte)((value / divisor4) % 85)];
                z85Dest[charNbr + 1] = z85Encoder[(byte)((value / divisor3) % 85)];
                z85Dest[charNbr + 2] = z85Encoder[(byte)((value / divisor2) % 85)];
                z85Dest[charNbr + 3] = z85Encoder[(byte)((value / divisor1) % 85)];
                z85Dest[charNbr + 4] = z85Encoder[(byte)(value % 85)];
                charNbr += 5;
            }

            if (remainder != 0)
            {
                // Take care of the remainder.
                value = 0;
                while (byteNbr < size)
                    value = (value * 256) + data[byteNbr++];

                var divisor = (uint)Math.Pow(85, remainder);
                while (divisor != 0)
                {
                    z85Dest[charNbr++] = z85Encoder[(char)((value / divisor) % 85)];
                    divisor /= 85;
                }
            }

            // Fast Span<char> to String cast.
            return z85Dest.ToString();
        }
    }
}
