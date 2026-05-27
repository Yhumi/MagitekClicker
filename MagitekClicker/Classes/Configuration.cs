using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using MagitekClicker;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace MagitekClicker.Classes;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled { get; set; } = false;
    public bool UseXIVSFXVolume { get; set; } = false;
    public float Volume { get; set; } = 0.5f;
    public List<AudioFile> AudioFiles = new();
    public List<Trigger> Triggers = new();
    public HashSet<XivChatType> AllowedChannels = new HashSet<XivChatType> { XivChatType.Say, XivChatType.FreeCompany, XivChatType.TellIncoming, XivChatType.Party, XivChatType.CrossParty, XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8};

    public void Save()
    {
        Service.PluginInterface.SavePluginConfig(this);
    }
}
