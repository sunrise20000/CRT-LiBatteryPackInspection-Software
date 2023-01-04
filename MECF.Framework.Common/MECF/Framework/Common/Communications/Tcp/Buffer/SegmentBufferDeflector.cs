using System;

namespace MECF.Framework.Common.Communications.Tcp.Buffer
{
	public class SegmentBufferDeflector
	{
		public static void AppendBuffer(ISegmentBufferManager bufferManager, ref ArraySegment<byte> receiveBuffer, int receiveCount, ref ArraySegment<byte> sessionBuffer, ref int sessionBufferCount)
		{
			if (sessionBuffer.Count < sessionBufferCount + receiveCount)
			{
				ArraySegment<byte> arraySegment = bufferManager.BorrowBuffer();
				if (arraySegment.Count < (sessionBufferCount + receiveCount) * 2)
				{
					bufferManager.ReturnBuffer(arraySegment);
					arraySegment = new ArraySegment<byte>(new byte[(sessionBufferCount + receiveCount) * 2]);
				}
				Array.Copy(sessionBuffer.Array, sessionBuffer.Offset, arraySegment.Array, arraySegment.Offset, sessionBufferCount);
				ArraySegment<byte> buffer = sessionBuffer;
				sessionBuffer = arraySegment;
				bufferManager.ReturnBuffer(buffer);
			}
			Array.Copy(receiveBuffer.Array, receiveBuffer.Offset, sessionBuffer.Array, sessionBuffer.Offset + sessionBufferCount, receiveCount);
			sessionBufferCount += receiveCount;
		}

		public static void ShiftBuffer(ISegmentBufferManager bufferManager, int shiftStart, ref ArraySegment<byte> sessionBuffer, ref int sessionBufferCount)
		{
			if (sessionBufferCount - shiftStart < shiftStart)
			{
				Array.Copy(sessionBuffer.Array, sessionBuffer.Offset + shiftStart, sessionBuffer.Array, sessionBuffer.Offset, sessionBufferCount - shiftStart);
				sessionBufferCount -= shiftStart;
				return;
			}
			ArraySegment<byte> buffer = bufferManager.BorrowBuffer();
			if (buffer.Count < sessionBufferCount - shiftStart)
			{
				bufferManager.ReturnBuffer(buffer);
				buffer = new ArraySegment<byte>(new byte[sessionBufferCount - shiftStart]);
			}
			Array.Copy(sessionBuffer.Array, sessionBuffer.Offset + shiftStart, buffer.Array, buffer.Offset, sessionBufferCount - shiftStart);
			Array.Copy(buffer.Array, buffer.Offset, sessionBuffer.Array, sessionBuffer.Offset, sessionBufferCount - shiftStart);
			sessionBufferCount -= shiftStart;
			bufferManager.ReturnBuffer(buffer);
		}

		public static void ReplaceBuffer(ISegmentBufferManager bufferManager, ref ArraySegment<byte> receiveBuffer, ref int receiveBufferOffset, int receiveCount)
		{
			if (receiveBufferOffset + receiveCount < receiveBuffer.Count)
			{
				receiveBufferOffset += receiveCount;
				return;
			}
			ArraySegment<byte> arraySegment = bufferManager.BorrowBuffer();
			if (arraySegment.Count < (receiveBufferOffset + receiveCount) * 2)
			{
				bufferManager.ReturnBuffer(arraySegment);
				arraySegment = new ArraySegment<byte>(new byte[(receiveBufferOffset + receiveCount) * 2]);
			}
			Array.Copy(receiveBuffer.Array, receiveBuffer.Offset, arraySegment.Array, arraySegment.Offset, receiveBufferOffset + receiveCount);
			receiveBufferOffset += receiveCount;
			ArraySegment<byte> buffer = receiveBuffer;
			receiveBuffer = arraySegment;
			bufferManager.ReturnBuffer(buffer);
		}
	}
}
