﻿using BattleRoyale.Utils;
using Netcode;
using StardewValley;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace BattleRoyale.Patches
{
    class CustomServerMessages : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Multiplayer), "processIncomingMessage");

        public static Stopwatch timer;
        public static int msThreshhold = 25;

        public static byte messageType = 0;
        public static byte[] messageData = new byte[0];


        public static bool Prefix(IncomingMessage msg)
        {
            timer = new();
            timer.Start();
            messageType = msg.MessageType;
            messageData = msg.Data;

            if (Game1.IsServer && (msg == null || !Game1.otherFarmers.ContainsKey(msg.FarmerID)))
            {
                //They have been kicked off the server
                return false;
            }

            if (msg.Reader == null)
            {
                //Data broke
                Console.WriteLine("Received null data");
                return false;
            }

            if (msg != null && msg.Data != null && msg.SourceFarmer != null && msg.MessageType == NetworkUtils.uniqueMessageType)
                return ProcessMessage(msg);

            return true;
        }

        public static void Postfix()
        {
            timer.Stop();

            if (timer.ElapsedMilliseconds > msThreshhold)
                ModEntry.BRGame.Monitor.Log($"Message exceed threshold: type {messageType}, time: {timer.ElapsedMilliseconds}ms", StardewModdingAPI.LogLevel.Error);
        }

        private static bool ProcessMessage(IncomingMessage msg)
        {
            byte[] msgData = msg.Data;
            Farmer sourceFarmer = msg.SourceFarmer;

            var subMessageType = (NetworkUtils.MessageTypes)BitConverter.ToInt32(msgData, 0);

            switch (subMessageType)
            {
                case NetworkUtils.MessageTypes.SEND_STORM_LOCATION_DATA:
                    try
                    {
                        var b = new BinaryFormatter();

                        var locationsReached = (Dictionary<string, DateTime>)b.Deserialize(new MemoryStream(msgData.Skip(4).ToArray()));

                        Console.WriteLine($"Received storm location data from server, info: Length={locationsReached.Count}");

                        Dictionary<GameLocation, DateTime> deserializedLocations = new();
                        foreach (var kvp in locationsReached)
                        {
                            GameLocation location = Game1.getLocationFromName(kvp.Key);
                            deserializedLocations[location] = kvp.Value;
                        }

                        Storm.TimeLocationWasReached = deserializedLocations;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unable to process storm location data. Kicking to prevent glitches...");
                        long id = sourceFarmer.UniqueMultiplayerID;
                        NetworkUtils.SendChatMessageToPlayerWithoutMod(id, "Unable to process storm location data. Kicking to prevent glitches...");
                        Game1.server.sendMessage(id, new OutgoingMessage((byte)19, id, Array.Empty<object>()));
                        Game1.server.playerDisconnected(id);
                        Game1.otherFarmers.Remove(id);
                    }
                    return false;
                case NetworkUtils.MessageTypes.SEND_MY_VERSION_TO_SERVER:
                    if (Game1.IsServer)
                    {
                        int major = BitConverter.ToInt32(msgData, 4);
                        int minor = BitConverter.ToInt32(msgData, 8);
                        byte[] sha = msgData.Skip(12).ToArray();

                        Console.WriteLine($"Received version from client {sourceFarmer.Name}/{sourceFarmer.UniqueMultiplayerID}: {major}.{minor}");
                        AutoKicker.AcknowledgeClientVersion(sourceFarmer.UniqueMultiplayerID, major, minor, sha);
                    }
                    return false;
            }

            return false;
        }


    }
}
