namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    /// <summary>
    /// Wraps a stream and automatically reads or writes null bytes to make the length of the stream a multiple of a specified
    /// alignment value.
    /// </summary>
    public class PaddedStream : Stream
    {
        private static readonly string TypeName = typeof(PaddedStream).FullName;

        private Stream stream;
        private long alignment;
        private long offset;
        private PaddedStreamMode mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaddedStream" /> class with the specified underlying stream.
        /// </summary>
        /// <param name="stream">The underlying stream.</param>
        /// <param name="alignment">
        /// Alignment of the stream. The length of the stream will be a multiple of the alignment.
        /// </param>
        /// <param name="mode">Operating mode for the <see cref="PaddedStream"/>.</param>
        public PaddedStream(Stream stream, long alignment, PaddedStreamMode mode)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            switch (mode)
            {
                case PaddedStreamMode.Read:
                case PaddedStreamMode.Write:
                    break;

                default:
                    throw new ArgumentOutOfRangeException("mode");
            }

            this.stream = stream;
            this.alignment = alignment;
            this.mode = mode;
        }

        public override bool CanRead
        {
            get
            {
                if (this.stream == null)
                {
                    throw new ObjectDisposedException(TypeName);
                }

                return this.mode == PaddedStreamMode.Read && this.stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                if (this.stream == null)
                {
                    throw new ObjectDisposedException(TypeName);
                }

                return this.stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (this.stream == null)
                {
                    throw new ObjectDisposedException(TypeName);
                }

                return this.mode == PaddedStreamMode.Write && this.stream.CanWrite;
            }
        }

        public override void Flush()
        {
            if (this.stream == null)
            {
                throw new ObjectDisposedException(TypeName);
            }

            if (this.mode != PaddedStreamMode.Write)
            {
                throw new NotSupportedException(Properties.Resources.PaddedStreamFlushRequiresWriteMode);
            }

            this.stream.Flush();
        }

        public override long Length
        {
            get
            {
                if (this.stream == null)
                {
                    throw new ObjectDisposedException(TypeName);
                }

                if (this.mode == PaddedStreamMode.Read)
                {
                    return this.AlignedLength(this.stream.Length);
                }

                return this.stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                if (this.stream == null)
                {
                    throw new ObjectDisposedException(TypeName);
                }

                return this.stream.Position + this.offset;
            }
            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.stream == null)
            {
                throw new ObjectDisposedException(TypeName);
            }

            if (this.mode != PaddedStreamMode.Read)
            {
                throw new NotSupportedException(Properties.Resources.PaddedStreamReadRequiresReadMode);
            }

            int bytesRead = 0;

            // If this.offset is not zero, we've already read past the end of the underlying stream, so don't bother reading
            // from it.
            if (this.offset == 0)
            {
                bytesRead = this.stream.Read(buffer, offset, count);
            }

            // Write zeroes in the buffer until the requested count is satisfied or until the end of the padded stream.
            while (bytesRead < count && this.Position < this.Length)
            {
                buffer[offset + bytesRead] = 0;
                ++bytesRead;
                ++this.offset;
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (this.stream == null)
            {
                throw new ObjectDisposedException(TypeName);
            }

            long position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = this.stream.Seek(offset, SeekOrigin.Begin);
                    this.offset = 0;
                    break;

                case SeekOrigin.Current:
                    position = this.stream.Seek(offset, SeekOrigin.Current);
                    this.offset = 0;
                    break;

                case SeekOrigin.End:
                    position = this.stream.Seek(offset + (this.Length - this.stream.Length), SeekOrigin.End);
                    this.offset = 0;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("origin");
            }

            return position;
        }

        /// <summary>
        /// Sets the length of the underlying stream.
        /// </summary>
        /// <param name="value">New length of the underlying stream.</param>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        /// <exception cref="NotSupportedException">
        /// The stream was not created in <see cref="PaddedStreamMode.Write"/> mode.
        /// </exception>
        public override void SetLength(long value)
        {
            if (this.stream == null)
            {
                throw new ObjectDisposedException(TypeName);
            }

            if (this.mode != PaddedStreamMode.Write)
            {
                throw new NotSupportedException(Properties.Resources.PaddedStreamSetLengthRequiresWriteMode);
            }

            this.stream.SetLength(value);
            this.offset = 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.stream == null)
            {
                throw new ObjectDisposedException(TypeName);
            }

            if (this.mode != PaddedStreamMode.Write)
            {
                throw new NotSupportedException(Properties.Resources.PaddedStreamWriteRequiresWriteMode);
            }

            this.stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="PaddedStream"/> and optionally releases the managed
        /// resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        /// <remarks>
        /// If the current <see cref="PaddedStream"/> is in <see cref="PaddedStreamMode.Write"/> mode, the underlying stream's
        /// length will be aligned to the alignment specified in the constructor. The underlying stream is not disposed.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && this.stream != null && this.mode == PaddedStreamMode.Write)
                {
                    long alignedLength = this.AlignedLength(this.stream.Length);
                    if (alignedLength > this.stream.Length)
                    {
                        // Pad the stream
                        this.stream.SetLength(alignedLength);
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Returns the first integer greater than or equal to <paramref name="length"/> that is a multiple of
        /// <see cref="alignment"/>.
        /// </summary>
        /// <param name="length">Length to align.</param>
        /// <returns>
        /// The first integer greater than or equal to <paramref name="length"/> that is a multiple of <see cref="alignment"/>.
        /// </returns>
        private long AlignedLength(long length)
        {
            long alignmentMinusOne = this.alignment - 1;

            // If this.alignment is a power of two...
            if ((this.alignment & alignmentMinusOne) == 0)
            {
                // ...then we can use a faster algorithm.
                return (length + alignmentMinusOne) & ~alignmentMinusOne;
            }

            return (length + (this.alignment - 1)) / this.alignment * this.alignment;
        }
    }
}
