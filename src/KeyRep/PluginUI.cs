using System;
using System.Drawing;
using System.Windows.Forms;
using VoK.Sdk.Plugins;

namespace KeyRep
{
    public class PluginUI : IPluginUI
    {
        private readonly Lazy<Image> _toolbarIcon;
        private readonly KeyRepForm _form;

        public PluginUI(KeyRepService repeat)
        {
            _form = new KeyRepForm(repeat);
            _toolbarIcon = new Lazy<Image>(LoadToolbarIcon);
        }

        public float? FocusedOpacity => 1.0f;

        public bool EnabledInCharacterSelection => false;

        public Image ToolbarImage => _toolbarIcon.Value;

        public object UserInterfaceForm => _form;

        public Tuple<int, int> MinSize => new(560, 580);

        private static Image LoadToolbarIcon()
        {
            var logo = BrandAssets.TryLoadLogoPng(36);
            return logo ?? BrandAssets.CreateFallbackToolbarIcon();
        }
    }
}
