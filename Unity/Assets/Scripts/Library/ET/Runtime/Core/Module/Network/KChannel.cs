﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ET
{

	
	public struct KcpWaitSendMessage
	{
		public long ActorId;
		public MessageObject Message;
	}
	
	public class KChannel : AChannel
	{
		private readonly KService Service;

		private Kcp kcp { get; set; }

		private readonly Queue<KcpWaitSendMessage> waitSendMessages = new Queue<KcpWaitSendMessage>();
		
		public readonly uint CreateTime;

		public uint LocalConn
		{
			get
			{
				return (uint)this.Id; 
			}
			private set
			{
				this.Id = value;
			}
		}
		public uint RemoteConn { get; set; }

		private readonly byte[] sendCache = new byte[2 * 1024];
		
		public bool IsConnected { get; set; }

		public string RealAddress { get; set; }
		
		private const int maxPacketSize = 10000;

		private readonly MemoryBuffer ms = new MemoryBuffer(maxPacketSize);

		private MemoryBuffer readMemory;
		private int needReadSplitCount;

		private IPEndPoint remoteAddress;

		public IPEndPoint RemoteAddress
		{
			get
			{
				return this.remoteAddress;
			}
			set
			{
				this.remoteAddress = new IPEndPointNonAlloc(value.Address, value.Port);
			}
		}

		private void InitKcp()
		{
			this.Service.KcpPtrChannels.Add(this.kcp.Id, this);
			
			switch (this.Service.ServiceType)
			{
				case ServiceType.Inner:
					this.kcp.Nodelay(1, 10, 2, 1);
					this.kcp.SetWndSize(1024, 1024);
					this.kcp.SetMtu(1400); // 默认1400
					this.kcp.SetMinrto(30);
					break;
				case ServiceType.Outer:
					this.kcp.Nodelay(1, 10, 2, 1);
					this.kcp.SetWndSize(256, 256);
					this.kcp.SetMtu(470);
					this.kcp.SetMinrto(30);
					break;
			}

		}
		
		// connect
		public KChannel(uint localConn, IPEndPoint remoteEndPoint, KService kService)
		{
			this.LocalConn = localConn;
			this.ChannelType = ChannelType.Connect;
			
			Log.Info($"channel create: {this.LocalConn} {remoteEndPoint} {this.ChannelType}");
			
			this.Service = kService;
			this.RemoteAddress = remoteEndPoint;
			this.CreateTime = kService.TimeNow;

			this.Connect(this.CreateTime);

		}

		// accept
		public KChannel(uint localConn, uint remoteConn, IPEndPoint remoteEndPoint, KService kService)
		{
			this.ChannelType = ChannelType.Accept;
			
			Log.Info($"channel create: {localConn} {remoteConn} {remoteEndPoint} {this.ChannelType}");

			this.Service = kService;
			this.LocalConn = localConn;
			this.RemoteConn = remoteConn;
			this.RemoteAddress = remoteEndPoint;
			this.kcp = new Kcp(this.RemoteConn, new IntPtr(this.Service.Id));
			this.InitKcp();
			
			this.CreateTime = kService.TimeNow;
		}
	

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			uint localConn = this.LocalConn;
			uint remoteConn = this.RemoteConn;
			Log.Info($"channel dispose: {localConn} {remoteConn} {this.Error}");
			
			long id = this.Id;
			this.Id = 0;
			this.Service.Remove(id);

			try
			{
				if (this.Error != ErrorCore.ERR_PeerDisconnect)
				{
					this.Service.Disconnect(localConn, remoteConn, this.Error, this.RemoteAddress, 3);
				}
			}
			catch (Exception e)
			{
				Log.Error(e);
			}

			if (this.kcp != null)
			{
				this.Service.KcpPtrChannels.Remove(this.kcp.Id);
				this.kcp.Release();
				this.kcp = null;
			}
		}

		public void HandleConnnect()
		{
			// 如果连接上了就不用处理了
			if (this.IsConnected)
			{
				return;
			}

			this.kcp = new Kcp(this.RemoteConn, new IntPtr(this.Service.Id));
			this.InitKcp();

			Log.Info($"channel connected: {this.LocalConn} {this.RemoteConn} {this.RemoteAddress}");
			this.IsConnected = true;
			
			while (true)
			{
				if (this.waitSendMessages.Count <= 0)
				{
					break;
				}
				
				KcpWaitSendMessage buffer = this.waitSendMessages.Dequeue();
				this.Send(buffer.ActorId, buffer.Message);
			}
		}

		private long lastConnectTime = long.MaxValue;

		/// <summary>
		/// 发送请求连接消息
		/// </summary>
		private void Connect(uint timeNow)
		{
			try
			{
				if (this.IsConnected)
				{
					return;
				}
				
				// 300毫秒后再次update发送connect请求
				if (timeNow < this.lastConnectTime + 300)
				{
					this.Service.AddToUpdate(300, this.Id);
					return;
				}
				
				// 10秒连接超时
				if (timeNow > this.CreateTime + KService.ConnectTimeoutTime)
				{
					Log.Error($"kChannel connect timeout: {this.Id} {this.RemoteConn} {timeNow} {this.CreateTime} {this.ChannelType} {this.RemoteAddress}");
					this.OnError(ErrorCore.ERR_KcpConnectTimeout);
					return;
				}
				
				byte[] buffer = sendCache;
				buffer.WriteTo(0, KcpProtocalType.SYN);
				buffer.WriteTo(1, this.LocalConn);
				buffer.WriteTo(5, this.RemoteConn);
				this.Service.Socket.SendTo(buffer, 0, 9, SocketFlags.None, this.RemoteAddress);
				// 这里很奇怪 调用socket.LocalEndPoint会动到this.RemoteAddressNonAlloc里面的temp，这里就不仔细研究了
				Log.Info($"kchannel connect {this.LocalConn} {this.RemoteConn} {this.RealAddress}");

				this.lastConnectTime = timeNow;

				this.Service.AddToUpdate(300, this.Id);
			}
			catch (Exception e)
			{
				Log.Error(e);
				this.OnError(ErrorCore.ERR_SocketCantSend);
			}
		}

		public void Update(uint timeNow)
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			// 如果还没连接上，发送连接请求
			if (!this.IsConnected && this.ChannelType == ChannelType.Connect)
			{
				this.Connect(timeNow);
				return;
			}

			if (this.kcp == null)
			{
				return;
			}
			
			try
			{
				this.kcp.Update(timeNow);
			}
			catch (Exception e)
			{
				Log.Error(e);
				this.OnError(ErrorCore.ERR_SocketError);
				return;
			}

			uint nextUpdateTime = this.kcp.Check(timeNow);
			this.Service.AddToUpdate(nextUpdateTime, this.Id);
		}

		public void HandleRecv(byte[] date, int offset, int length)
		{
			if (this.IsDisposed)
			{
				return;
			}

			this.kcp.Input(date, offset, length);
			this.Service.AddToUpdate(0, this.Id);

			while (true)
			{
				if (this.IsDisposed)
				{
					break;
				}
				int n = this.kcp.Peeksize();
				if (n < 0)
				{
					break;
				}
				if (n == 0)
				{
					this.OnError((int)SocketError.NetworkReset);
					return;
				}

				if (this.needReadSplitCount > 0) // 说明消息分片了
				{
					byte[] buffer = readMemory.GetBuffer();
					int count = this.kcp.Recv(buffer, (int)this.readMemory.Length - this.needReadSplitCount, n);
					this.needReadSplitCount -= count;
					if (n != count)
					{
						Log.Error($"kchannel read error1: {this.LocalConn} {this.RemoteConn}");
						this.OnError(ErrorCore.ERR_KcpReadNotSame);
						return;
					}
					
					if (this.needReadSplitCount < 0)
					{
						Log.Error($"kchannel read error2: {this.LocalConn} {this.RemoteConn}");
						this.OnError(ErrorCore.ERR_KcpSplitError);
						return;
					}
										
					// 没有读完
					if (this.needReadSplitCount != 0)
					{
						continue;
					}
				}
				else
				{
					this.readMemory = this.ms;
					this.readMemory.SetLength(n);
					this.readMemory.Seek(0, SeekOrigin.Begin);
					
					byte[] buffer = readMemory.GetBuffer();
					int count = this.kcp.Recv(buffer, 0, n);
					if (n != count)
					{
						break;
					}
					
					// 判断是不是分片
					if (n == 8)
					{
						int headInt = BitConverter.ToInt32(this.readMemory.GetBuffer(), 0);
						if (headInt == 0)
						{
							this.needReadSplitCount = BitConverter.ToInt32(readMemory.GetBuffer(), 4);
							if (this.needReadSplitCount <= maxPacketSize)
							{
								Log.Error($"kchannel read error3: {this.needReadSplitCount} {this.LocalConn} {this.RemoteConn}");
								this.OnError(ErrorCore.ERR_KcpSplitCountError);
								return;
							}
							this.readMemory = new MemoryBuffer(this.needReadSplitCount);
							this.readMemory.SetLength(this.needReadSplitCount);
							this.readMemory.Seek(0, SeekOrigin.Begin);
							continue;
						}
					}
				}


				switch (this.Service.ServiceType)
				{
					case ServiceType.Inner:
						this.readMemory.Seek(Packet.ActorIdLength + Packet.OpcodeLength, SeekOrigin.Begin);
						break;
					case ServiceType.Outer:
						this.readMemory.Seek(Packet.OpcodeLength, SeekOrigin.Begin);
						break;
				}
				MemoryBuffer mem = this.readMemory;
				this.readMemory = null;
				this.OnRead(mem);
			}
		}

		public void Output(IntPtr bytes, int count)
		{
			if (this.IsDisposed)
			{
				return;
			}
			try
			{				
				// 没连接上 kcp不往外发消息, 其实本来没连接上不会调用update，这里只是做一层保护
				if (!this.IsConnected)
				{
					return;
				}
				
				if (count == 0)
				{
					Log.Error($"output 0");
					return;
				}

				byte[] buffer = this.sendCache;
				buffer.WriteTo(0, KcpProtocalType.MSG);
				// 每个消息头部写下该channel的id;
				buffer.WriteTo(1, this.LocalConn);
				Marshal.Copy(bytes, buffer, 5, count);
				this.Service.Socket.SendTo(buffer, 0, count + 5, SocketFlags.None, this.RemoteAddress);
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}

        private void KcpSend(long actorId, MemoryBuffer memoryStream)
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			switch (this.Service.ServiceType)
			{
				case ServiceType.Inner:
				{
					memoryStream.GetBuffer().WriteTo(0, actorId);
					break;
				}
				case ServiceType.Outer:
				{
					// 外网不需要发送actorId，跳过
					memoryStream.Seek(Packet.ActorIdLength, SeekOrigin.Begin);
					break;
				}
			}
			int count = (int) (memoryStream.Length - memoryStream.Position);

			// 超出maxPacketSize需要分片
			if (count <= maxPacketSize)
			{
				this.kcp.Send(memoryStream.GetBuffer(), (int)memoryStream.Position, count);
			}
			else
			{
				// 先发分片信息
				this.sendCache.WriteTo(0, 0);
				this.sendCache.WriteTo(4, count);
				this.kcp.Send(this.sendCache, 0, 8);

				// 分片发送
				int alreadySendCount = 0;
				while (alreadySendCount < count)
				{
					int leftCount = count - alreadySendCount;
					
					int sendCount = leftCount < maxPacketSize? leftCount: maxPacketSize;
					
					this.kcp.Send(memoryStream.GetBuffer(), (int)memoryStream.Position + alreadySendCount, sendCount);
					
					alreadySendCount += sendCount;
				}
			}

			this.Service.AddToUpdate(0, this.Id);
		}
		
		public void Send(long actorId, MessageObject message)
		{
			if (!this.IsConnected)
			{
				KcpWaitSendMessage kcpWaitSendMessage = new() { ActorId = actorId, Message = message };
				this.waitSendMessages.Enqueue(kcpWaitSendMessage);
				return;
			}
			
			MemoryBuffer memoryStream = this.Service.Fetch(message);

			if (this.kcp == null)
			{
				throw new Exception("kchannel connected but kcp is zero!");
			}
			
			// 检查等待发送的消息，如果超出最大等待大小，应该断开连接
			int n = this.kcp.Waitsnd();
			int maxWaitSize = 0;
			switch (this.Service.ServiceType)
			{
				case ServiceType.Inner:
					maxWaitSize = Kcp.InnerMaxWaitSize;
					break;
				case ServiceType.Outer:
					maxWaitSize = Kcp.OuterMaxWaitSize;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			if (n > maxWaitSize)
			{
				Log.Error($"kcp wait snd too large: {n}: {this.LocalConn} {this.RemoteConn}");
				this.OnError(ErrorCore.ERR_KcpWaitSendSizeTooLarge);
				return;
			}

			this.KcpSend(actorId, memoryStream);
		}
		
		private void OnRead(MemoryBuffer memoryStream)
		{
			try
			{
				long channelId = this.Id;
				object message = null;
				long actorId = 0;
				switch (this.Service.ServiceType)
				{
					case ServiceType.Outer:
					{
						ushort opcode = BitConverter.ToUInt16(memoryStream.GetBuffer(), Packet.KcpOpcodeIndex);
						Type type = NetServices.Instance.GetType(opcode);
						message = MessageSerializeHelper.Deserialize(type, memoryStream);
						break;
					}
					case ServiceType.Inner:
					{
						actorId = BitConverter.ToInt64(memoryStream.GetBuffer(), Packet.ActorIdIndex);
						ushort opcode = BitConverter.ToUInt16(memoryStream.GetBuffer(), Packet.OpcodeIndex);
						Type type = NetServices.Instance.GetType(opcode);
						message = MessageSerializeHelper.Deserialize(type, memoryStream);
						break;
					}
				}
				NetServices.Instance.OnRead(this.Service.Id, channelId, actorId, message);
			}
			catch (Exception e)
			{
				Log.Error(e);
				this.OnError(ErrorCore.ERR_PacketParserError);
			}
		}
		
		public void OnError(int error)
		{
			long channelId = this.Id;
			this.Service.Remove(channelId, error);
			NetServices.Instance.OnError(this.Service.Id, channelId, error);
		}
	}
}
