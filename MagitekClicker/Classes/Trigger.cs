using Dalamud.Game.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagitekClicker.Classes
{
    [Serializable]
    public class Trigger(string name)
    {
        public string Name { get; set; } = name;
        public bool Enabled { get; set; } = false;
        public List<string> TriggerPhrases { get; set; } = new();
        public List<string> AudioIds { get; set; } = new();
        public HashSet<XivChatType> AllowedChannels { get; set; } = new();
    }
}
