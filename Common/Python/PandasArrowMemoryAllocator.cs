


using System;
using System.Buffers;
using System.Collections.Generic;
using Apache.Arrow.Memory;

namespace QuantConnect.Python
{
    public class PandasArrowMemoryAllocator : NativeMemoryAllocator
    {
        private List<PandasMemoryOwner> _free = new List<PandasMemoryOwner>();
        private List<PandasMemoryOwner> _used = new List<PandasMemoryOwner>();

        public PandasArrowMemoryAllocator() : base()
        {
        }

        protected override IMemoryOwner<byte> AllocateInternal(int length, out int bytesAllocated)
        {
            PandasMemoryOwner owner = null;
            if (_free.Count == 0)
            {
                owner = new PandasMemoryOwner(base.AllocateInternal(length, out bytesAllocated));
                _used.Add(owner);

                return owner;
            }

            for (var i = 0; i < _free.Count; i++)
            {
                var memory = _free[i];
                if (length < memory.Original.Memory.Length)
                {
                    owner = memory;
                    bytesAllocated = 0;
                    _free.Remove(owner);
                    _used.Add(owner);
                    owner.RestoreSlice();
                    owner.Slice(0, length);
                    return owner;
                }

                if (memory.Original.Memory.Length == length)
                {
                    owner = memory;
                    bytesAllocated = 0;
                    _free.Remove(owner);
                    _used.Add(owner);
                    owner.RestoreSlice();
                    return owner;
                }
            }

            owner = new PandasMemoryOwner(base.AllocateInternal(length, out bytesAllocated));
            _used.Add(owner);

            return owner;
        }

        public void Free()
        {
            foreach (var used in _used)
            {
                used.Memory.Span.Fill(0);
                _free.Add(used);
            }

            _used.Clear();
        }

        public class PandasMemoryOwner : IMemoryOwner<byte>, IDisposable
        {
            public IMemoryOwner<byte> Original { get; }
            public Memory<byte> Memory { get; private set; }

            public PandasMemoryOwner(IMemoryOwner<byte> memory)
            {
                Original = memory;
                Memory = Original.Memory;
            }

            public void Slice(int start, int length)
            {
                Memory = Original.Memory.Slice(start, length);
            }

            public void RestoreSlice()
            {
                Memory = Original.Memory;
            }

            public void Dispose()
            {
                Original.Dispose();
            }
        }
    }
}
