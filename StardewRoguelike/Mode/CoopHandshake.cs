using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace StardewRoguelike.Mode
{
    /// <summary>
    /// Agrees with the other side of a multiplayer connection on which game is being
    /// played, so a farmhand joining a roguelike host ends up in roguelike mode and a
    /// farmhand joining a vanilla farm does not.
    ///
    /// This has to settle before the client finishes pulling down the save: the mode
    /// decides whether Maps/Mine is the lobby and whether the gameplay patches are
    /// applied at all. The host therefore announces its mode as soon as a peer
    /// appears, rather than waiting to be asked.
    /// </summary>
    internal static class CoopHandshake
    {
        public const string MessageType = "RoguelikeModeHandshake";

        /// <summary>Told to the other side; a record so SMAPI can round-trip it as JSON.</summary>
        public class ModePayload
        {
            public bool Active { get; set; }
        }

        /// <summary>
        /// Fires on both ends when peer details are exchanged, which is the earliest
        /// either side knows the other exists.
        /// </summary>
        public static void PeerContextReceived(object? sender, PeerContextReceivedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                Announce(e.Peer.PlayerID);
                return;
            }

            // A host without the mod installed cannot be running a roguelike, so
            // there is nothing to wait for and we stay vanilla.
            if (e.Peer.IsHost && e.Peer.GetMod(ModEntry.ModId) is null)
            {
                ModEntry.ModMonitor.Log("Host is not running the roguelike; staying in vanilla.", LogLevel.Debug);
                RoguelikeMode.Apply(false);
            }
        }

        private static void Announce(long peerId)
        {
            ModEntry.MultiplayerHelper.SendMessage(
                new ModePayload { Active = RoguelikeMode.IsActive },
                MessageType,
                modIDs: new[] { ModEntry.ModId },
                playerIDs: new[] { peerId });
        }

        /// <summary>
        /// Applies the host's mode on a farmhand.
        ///
        /// Deliberately immediate rather than deferred to the update loop: the save
        /// is on its way and the patches have to be in place before it arrives. This
        /// runs from a SMAPI event rather than from inside one of our patches, so
        /// there is no patched frame on the stack to pull out from under.
        /// </summary>
        public static void Receive(ModMessageReceivedEventArgs e)
        {
            ModePayload payload = e.ReadAs<ModePayload>();

            if (Context.IsMainPlayer)
                return;

            ModEntry.ModMonitor.Log($"Host reports roguelike mode: {payload.Active}", LogLevel.Debug);
            RoguelikeMode.Apply(payload.Active);
        }
    }
}
