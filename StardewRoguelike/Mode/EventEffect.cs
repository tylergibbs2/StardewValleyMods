using StardewRoguelike.Bosses;
using StardewRoguelike.ChallengeFloors;

namespace StardewRoguelike.Mode
{
    /// <summary>
    /// Subscribes and unsubscribes the handlers that drive a run.
    ///
    /// The handlers that have to survive a return to vanilla are registered once in
    /// ModEntry.Entry and are not listed here: the config menu, the update check,
    /// the asset handler (which declines to replace anything when inactive) and the
    /// multiplayer channel, which is how a farmhand finds out the host is running a
    /// roguelike in the first place.
    /// </summary>
    internal sealed class EventEffect : IModeEffect
    {
        public string Name => "events";

        public void Activate()
        {
            var events = ModEntry.Events;
            ModEntry entry = ModEntry.Instance;

            events.GameLoop.SaveLoaded += Roguelike.SaveLoaded;
            events.GameLoop.TimeChanged += Roguelike.TimeChanged;

            events.GameLoop.UpdateTicked += Stats.WatchChanges;
            events.GameLoop.UpdateTicked += Roguelike.UpdateTicked;
            events.GameLoop.UpdateTicked += SpectatorMode.Update;
            events.GameLoop.UpdateTicked += Curse.UpdateTicked;

            events.Display.MenuChanged += entry.MenuChanged;
            events.Display.MenuChanged += Roguelike.MenuChanged;
            events.Display.RenderedHud += entry.RenderHud;

            events.Player.Warped += Roguelike.PlayerWarped;
            events.Player.Warped += Merchant.PlayerWarped;
            events.Player.Warped += ChallengeFloor.PlayerWarped;
            events.Player.Warped += BossManager.PlayerWarped;
            events.Player.Warped += SpectatorMode.RespawnPlayers;

            events.Input.ButtonPressed += Roguelike.ButtonPressed;
        }

        public void Deactivate()
        {
            var events = ModEntry.Events;
            ModEntry entry = ModEntry.Instance;

            events.GameLoop.SaveLoaded -= Roguelike.SaveLoaded;
            events.GameLoop.TimeChanged -= Roguelike.TimeChanged;

            events.GameLoop.UpdateTicked -= Stats.WatchChanges;
            events.GameLoop.UpdateTicked -= Roguelike.UpdateTicked;
            events.GameLoop.UpdateTicked -= SpectatorMode.Update;
            events.GameLoop.UpdateTicked -= Curse.UpdateTicked;

            events.Display.MenuChanged -= entry.MenuChanged;
            events.Display.MenuChanged -= Roguelike.MenuChanged;
            events.Display.RenderedHud -= entry.RenderHud;

            events.Player.Warped -= Roguelike.PlayerWarped;
            events.Player.Warped -= Merchant.PlayerWarped;
            events.Player.Warped -= ChallengeFloor.PlayerWarped;
            events.Player.Warped -= BossManager.PlayerWarped;
            events.Player.Warped -= SpectatorMode.RespawnPlayers;

            events.Input.ButtonPressed -= Roguelike.ButtonPressed;
        }
    }
}
