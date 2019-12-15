﻿namespace CoenM.Encoding.Test.Z85vsBase64.Decode
{
    using System;
    using System.Buffers;

    using JetBrains.Annotations;

    public class Z85Base64DecodeResult
    {
        private readonly OperationStatus status;

        public Z85Base64DecodeResult(OperationStatus status, int charactersBlocksConsumed, bool allCharsConsumed, int byteBlocksWritten)
        {
            this.status = status;
            CharactersBlocksConsumed = charactersBlocksConsumed;
            AllCharsConsumed = allCharsConsumed;
            ByteBlocksWritten = byteBlocksWritten;
        }

        public string Status => status.ToString();

        public int CharactersBlocksConsumed { get; }

        public bool AllCharsConsumed { get; }

        public int ByteBlocksWritten { get; }

        public override string ToString()
        {
            return $"{nameof(Status)}: {Status}{Environment.NewLine}{nameof(CharactersBlocksConsumed)}: {CharactersBlocksConsumed}{Environment.NewLine}{nameof(AllCharsConsumed)}: {AllCharsConsumed}{Environment.NewLine}{nameof(ByteBlocksWritten)}: {ByteBlocksWritten}";
        }
    }
}
