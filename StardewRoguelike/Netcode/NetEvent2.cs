﻿using System.IO;
using Netcode;

namespace StardewRoguelike.Netcode
{
	public class NetEvent2<T1, T2> : AbstractNetEvent2<T1, T2> where T1 : NetEventArg, new() where T2 : NetEventArg, new()
	{
		protected override T1 readEventArg1(BinaryReader reader, NetVersion version)
		{
			T1 arg = new();
			arg.Read(reader);
			return arg;
		}

		protected override void writeEventArg1(BinaryWriter writer, T1 eventArg)
		{
			eventArg.Write(writer);
		}

		protected override T2 readEventArg2(BinaryReader reader, NetVersion version)
		{
			T2 arg = new();
			arg.Read(reader);
			return arg;
		}

		protected override void writeEventArg2(BinaryWriter writer, T2 eventArg)
		{
			eventArg.Write(writer);
		}
	}
}
