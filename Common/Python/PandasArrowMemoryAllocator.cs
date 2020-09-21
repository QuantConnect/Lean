


using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Apache.Arrow.Memory;
using QuantConnect.Logging;

namespace QuantConnect.Python
{
    public class PandasArrowMemoryAllocator : NativeMemoryAllocator, IDisposable
    {
        private bool _disposed;
        private List<PandasMemoryOwner> _free = new List<PandasMemoryOwner>();
        private List<PandasMemoryOwner> _used = new List<PandasMemoryOwner>();

        public PandasArrowMemoryAllocator() : base()
        {
        }

        protected override IMemoryOwner<byte> AllocateInternal(int length, out int bytesAllocated)
        {
            PandasMemoryOwner owner;
            var memoryResizeIndexes = new List<KeyValuePair<int, int>>();

            for (var i = 0; i < _free.Count; i++)
            {
                var memory = _free[i];
                if (length > memory.Original.Memory.Length)
                {
                    memoryResizeIndexes.Add(new KeyValuePair<int, int>(i, memory.Original.Memory.Length));
                    continue;
                }

                owner = memory;
                bytesAllocated = 0;

                _free.Remove(owner);
                _used.Add(owner);
                owner.Reset();

                if (length != memory.Original.Memory.Length)
                {
                    owner.Slice(0, length);
                }

                return owner;
            }

            if (memoryResizeIndexes.Count != 0)
            {
                // Get the smallest resizable instance, and reallocate a larger buffer.
                var resizeIndex = memoryResizeIndexes.OrderBy(x => x.Value).First();
                var resizable = _free[resizeIndex.Key];

                resizable.Resize(base.AllocateInternal(length, out bytesAllocated));

                _used.Add(resizable);
                _free.RemoveAt(resizeIndex.Key);

                return resizable;
            }

            // New allocation, should only be called a few times when we start using the allocator
            owner = new PandasMemoryOwner(base.AllocateInternal(length, out bytesAllocated));
            _used.Add(owner);

            return owner;
        }

        /// <summary>
        /// Frees the underlying memory buffers so that they can be re-used
        /// </summary>
        public void Free()
        {
            foreach (var used in _used)
            {
                _free.Add(used);
            }

            _used.Clear();
        }

        private class PandasMemoryOwner : IMemoryOwner<byte>
        {
            private bool _disposed;

            /// <summary>
            /// Original memory owner containing the full-length byte-array
            /// we initially allocated.
            /// </summary>
            public IMemoryOwner<byte> Original { get; private set; }

            /// <summary>
            /// Slice of the original memory owner containing the contents of
            /// the buffer Arrow will use. We slice the original memory so
            /// that Arrow doesn't panic when it receives a slice with a length
            /// longer than it expects when serializing its internal buffer.
            /// </summary>
            public Memory<byte> Memory { get; private set; }

            public PandasMemoryOwner(IMemoryOwner<byte> memory)
            {
                Original = memory;
                Memory = Original.Memory;
            }

            /// <summary>
            /// Creates a slice of the original MemoryOwner and stores the result in <see cref="Memory"/>
            /// </summary>
            /// <param name="start">Index start of the slice</param>
            /// <param name="length">Length of the slice</param>
            public void Slice(int start, int length)
            {
                Memory = Original.Memory.Slice(start, length);
            }

            /// <summary>
            /// Restores the <see cref="Memory"/> slice to its initial value
            /// </summary>
            public void Reset()
            {
                Memory = Original.Memory;
            }

            /// <summary>
            /// Resizes the instance to the new memory size
            /// </summary>
            /// <param name="newMemory"></param>
            public void Resize(IMemoryOwner<byte> newMemory)
            {
                Original.Dispose();
                Original = newMemory;
                Memory = Original.Memory;
            }

            public void Free()
            {
                Original.Dispose();
                Memory = null;
                Original = null;
            }

            /// <summary>
            /// no-op dispose because we want to re-use the MemoryOwner instance after we dispose of a RecordBatch.
            /// To dispose of the resources this class owns, use <see cref="Free"/>
            /// </summary>
            public void Dispose()
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("PandasArrowMemoryAllocator has already been disposed");
            }
            foreach (var free in _free)
            {
                free.Free();
            }
            foreach (var used in _used)
            {
                used.Free();
            }

            _free.Clear();
            _used.Clear();

            _disposed = true;
        }
    }
}
