using System;
using VoK.Sdk;
using VoK.Sdk.Ddo;
using VoK.Sdk.Plugins;

namespace KeyRep
{
    public class Plugin : IDdoPlugin
    {
        public Guid PluginId => Guid.Parse("f4c8a901-2d7e-4b1f-9c60-8e5a3d2b1c00");

        public GameId Game => GameId.DDO;

        public string PluginKey => "7a91e4f0-6c2d-4b8a-9e1f-3050d4c8b2a1";

        public string Name => "KeyRep";

        public string Description => "Repeats a DDO hotbar/SendInput command or a Windows key chord on a timer.";

        public string Author => "Truecalling";

        public Version Version => GetType().Assembly.GetName().Version!;

        internal static Plugin? Instance { get; private set; }

        private KeyRepService? _repeat;
        private PluginUI? _ui;

        public Plugin()
        {
            Instance = this;
        }

        public void Initialize(IDdoGameDataProvider gameDataProvider, string folder)
        {
            _repeat = new KeyRepService(gameDataProvider);
            _ui = new PluginUI(_repeat);
        }

        public IPluginUI GetPluginUI()
        {
            return _ui ?? throw new InvalidOperationException("Plugin not initialized.");
        }

        public void Terminate()
        {
            _repeat?.Dispose();
            _repeat = null;
            _ui = null;
            Instance = null;
        }
    }
}
