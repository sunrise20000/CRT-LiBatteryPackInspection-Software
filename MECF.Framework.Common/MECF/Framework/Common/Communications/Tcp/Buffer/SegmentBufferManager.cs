using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MECF.Framework.Common.Communications.Tcp.Buffer
{
	public class SegmentBufferManager : ISegmentBufferManager
	{
		private const int TrialsCount = 100;

		private static SegmentBufferManager _defaultBufferManager;

		private readonly int _segmentChunks;

		private readonly int _chunkSize;

		private readonly int _segmentSize;

		private readonly bool _allowedToCreateMemory;

		private readonly ConcurrentStack<ArraySegment<byte>> _buffers = new ConcurrentStack<ArraySegment<byte>>();

		private readonly List<byte[]> _segments;

		private readonly object _creatingNewSegmentLock = new object();

		public static SegmentBufferManager Default
		{
			get
			{
				if (_defaultBufferManager == null)
				{
					_defaultBufferManager = new SegmentBufferManager(1024, 1024, 1);
				}
				return _defaultBufferManager;
			}
		}

		public int ChunkSize => _chunkSize;

		public int SegmentsCount => _segments.Count;

		public int SegmentChunksCount => _segmentChunks;

		public int AvailableBuffers => _buffers.Count;

		public int TotalBufferSize => _segments.Count * _segmentSize;

		public static void SetDefaultBufferManager(SegmentBufferManager manager)
		{
			if (manager == null)
			{
				throw new ArgumentNullException("manager");
			}
			_defaultBufferManager = manager;
		}

		public SegmentBufferManager(int segmentChunks, int chunkSize)
			: this(segmentChunks, chunkSize, 1)
		{
		}

		public SegmentBufferManager(int segmentChunks, int chunkSize, int initialSegments)
			: this(segmentChunks, chunkSize, initialSegments, allowedToCreateMemory: true)
		{
		}

		public SegmentBufferManager(int segmentChunks, int chunkSize, int initialSegments, bool allowedToCreateMemory)
		{
			if (segmentChunks <= 0)
			{
				throw new ArgumentException("segmentChunks");
			}
			if (chunkSize <= 0)
			{
				throw new ArgumentException("chunkSize");
			}
			if (initialSegments < 0)
			{
				throw new ArgumentException("initialSegments");
			}
			_segmentChunks = segmentChunks;
			_chunkSize = chunkSize;
			_segmentSize = _segmentChunks * _chunkSize;
			_segments = new List<byte[]>();
			_allowedToCreateMemory = true;
			for (int i = 0; i < initialSegments; i++)
			{
				CreateNewSegment(forceCreation: true);
			}
			_allowedToCreateMemory = allowedToCreateMemory;
		}

		private void CreateNewSegment(bool forceCreation)
		{
			if (!_allowedToCreateMemory)
			{
				throw new UnableToCreateMemoryException();
			}
			lock (_creatingNewSegmentLock)
			{
				if (forceCreation || _buffers.Count <= _segmentChunks / 2)
				{
					byte[] array = new byte[_segmentSize];
					_segments.Add(array);
					for (int i = 0; i < _segmentChunks; i++)
					{
						ArraySegment<byte> item = new ArraySegment<byte>(array, i * _chunkSize, _chunkSize);
						_buffers.Push(item);
					}
				}
			}
		}

		public ArraySegment<byte> BorrowBuffer()
		{
			for (int i = 0; i < 100; i++)
			{
				if (_buffers.TryPop(out var result))
				{
					return result;
				}
				CreateNewSegment(forceCreation: false);
			}
			throw new UnableToAllocateBufferException();
		}

		public IEnumerable<ArraySegment<byte>> BorrowBuffers(int count)
		{
			ArraySegment<byte>[] array = new ArraySegment<byte>[count];
			int i = 0;
			int j = 0;
			try
			{
				for (; i < 100; i++)
				{
					for (; j < count; j++)
					{
						if (!_buffers.TryPop(out var result))
						{
							break;
						}
						array[j] = result;
					}
					if (j == count)
					{
						return array;
					}
					CreateNewSegment(forceCreation: false);
				}
				throw new UnableToAllocateBufferException();
			}
			catch
			{
				if (j > 0)
				{
					ReturnBuffers(array.Take(j));
				}
				throw;
			}
		}

		public void ReturnBuffer(ArraySegment<byte> buffer)
		{
			if (ValidateBuffer(buffer))
			{
				_buffers.Push(buffer);
			}
		}

		public void ReturnBuffers(IEnumerable<ArraySegment<byte>> buffers)
		{
			if (buffers == null)
			{
				throw new ArgumentNullException("buffers");
			}
			foreach (ArraySegment<byte> buffer in buffers)
			{
				if (ValidateBuffer(buffer))
				{
					_buffers.Push(buffer);
				}
			}
		}

		public void ReturnBuffers(params ArraySegment<byte>[] buffers)
		{
			if (buffers == null)
			{
				throw new ArgumentNullException("buffers");
			}
			foreach (ArraySegment<byte> arraySegment in buffers)
			{
				if (ValidateBuffer(arraySegment))
				{
					_buffers.Push(arraySegment);
				}
			}
		}

		private bool ValidateBuffer(ArraySegment<byte> buffer)
		{
			if (buffer.Array == null || buffer.Count == 0 || buffer.Array.Length < buffer.Offset + buffer.Count)
			{
				return false;
			}
			if (buffer.Count != _chunkSize)
			{
				return false;
			}
			return true;
		}
	}
}
