using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using MagitekClicker.Classes;

namespace MagitekClicker.Windows;

public class MainWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    private string Search = string.Empty;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Magitek Clicker##clickermain")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Configuration = plugin.Configuration;
        Configuration.Save();

        Plugin = plugin;
    }

    public void Dispose() {
        if (this.IsOpen) this.Toggle();
        Configuration.Save();
    }

    public override void Draw()
    {
        if(ImGui.BeginTabBar("Tab Bar##clickertabmain", ImGuiTabBarFlags.None))
        {
            DrawGeneralTab();
            DrawSoundsTab();
            DrawTriggersTab();

            ImGui.EndTabBar();
        }
    }

    private void DrawGeneralTab()
    {
        if(ImGui.BeginTabItem("General"))
        {
            var isEnabled = Configuration.Enabled;
            if (ImGui.Checkbox("Enable clicker?", ref isEnabled))
            {
                Configuration.Enabled = isEnabled;
                Configuration.Save();
            }

            var volume = Configuration.Volume;
            if(ImGui.SliderFloat("Volume", ref volume, 0f, 1f))
            {
                Configuration.Volume = volume;
                Configuration.Save();
            }

            ImGui.Separator();

            ImGui.TextWrapped("Planned future features:");
            ImGui.TextWrapped(" - Support for more audio formats (.ogg, etc)");
            ImGui.TextWrapped(" - Change which channels are allowed to trigger phrases, including on a per-phrase basis");
            ImGui.TextWrapped(" - Restrict phrases to specific players or groups of players");
            ImGui.TextWrapped(" - Assign more than one sound to a phrase and choose one at random");

            ImGui.EndTabItem();
        }
    }

    private void DrawSoundsTab()
    {
        if (ImGui.BeginTabItem("Sounds"))
        {
            ImGui.TextWrapped("Add the path to sound files on your computer below. The name is the ID used to identify the sound when setting up triggers. Note that only .mp3 and .wav files are currently supported.");
            ImGui.Separator();

            if (ImGui.Button("New Sound"))
            {
                string name = $"Sound {Configuration.AudioFiles.Count}";
                AudioFile audioFile = new AudioFile(name);
                Configuration.AudioFiles.Add(audioFile);
                Configuration.Save();
            }
            if (ImGui.BeginTable("##Sounds", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Path");
                ImGui.TableSetupColumn("Delete");
                ImGui.TableHeadersRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                for (int i = 0; i < Configuration.AudioFiles.Count; i++)
                {
                    AudioFile audioFile = Configuration.AudioFiles[i];

                    string name = audioFile.Name;

                    if (ImGui.InputTextWithHint($"##sound-name{i}", "", ref name, 100))
                    {
                        audioFile.Name = name;
                        Configuration.Save();
                    }

                    ImGui.TableNextColumn();

                    string path = audioFile.Path;
                    if (ImGui.InputTextWithHint($"##sound-path{i}", "", ref path, 100))
                    {
                        audioFile.Path = path;
                        Configuration.Save();
                    }

                    ImGui.TableNextColumn();

                    if(ImGui.Button($"Delete##sound-delete{i}"))
                    {
                        Configuration.AudioFiles.RemoveAt(i);
                        Configuration.Save();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                }

            }
            ImGui.EndTable();

            ImGui.EndTabItem();
        }
    }

    private void DrawTriggersTab()
    {
        if (ImGui.BeginTabItem("Triggers"))
        {
            ImGui.TextWrapped("Add trigger phrases below and the sound they should correspond to - use the name given to the sound in the Sounds tab, not the path to the file.");
            ImGui.TextWrapped("Optionally, select channel(s) the trigger can be used in. Select none to use the global filter.");
            ImGui.Separator();

            if (ImGui.Button("New Trigger"))
            {
                string name = $"Trigger {Configuration.Triggers.Count}";
                Trigger trigger = new Trigger(name);
                Configuration.Triggers.Add(trigger);
                Configuration.Save();
            }
            if (ImGui.BeginTable("##Triggers", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn("Phrase", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn("Sound", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn("Channels", ImGuiTableColumnFlags.WidthStretch, 3);
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthStretch, 1);
                ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.WidthStretch, 1);
                ImGui.TableHeadersRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                for (int i = 0; i < Configuration.Triggers.Count; i++)
                {
                    var trigger = Configuration.Triggers[i];

                    string name = trigger.Name;

                    if (ImGui.InputTextWithHint($"##trigger-name{i}", "", ref name, 100))
                    {
                        trigger.Name = name;
                        Configuration.Save();
                    }

                    ImGui.TableNextColumn();

                    string phrase = trigger.TriggerPhrases.Count > 0 ? trigger.TriggerPhrases[0] : "";
                    if (ImGui.InputTextWithHint($"##trigger-phrase{i}", "", ref phrase, 100))
                    {
                        if (trigger.TriggerPhrases.Count == 0) trigger.TriggerPhrases.Add(phrase);
                        else trigger.TriggerPhrases[0] = phrase;
                        Configuration.Save();
                    }

                    ImGui.TableNextColumn();

                    string sound = trigger.AudioIds.Count > 0 ? trigger.AudioIds[0] : "";
                    if (ImGui.InputTextWithHint($"##trigger-sound{i}", "", ref sound, 100))
                    {
                        if (trigger.AudioIds.Count == 0) trigger.AudioIds.Add(sound);
                        else trigger.AudioIds[0] = sound;
                        Configuration.Save();
                    }

                    ImGui.TableNextColumn();

                    string allowedChannelsPreview = 
                        trigger.AllowedChannels.Count > 1 ? $"{trigger.AllowedChannels.First().ToString()} (+{trigger.AllowedChannels.Count} more)" : 
                        trigger.AllowedChannels.Count == 1 ? trigger.AllowedChannels.First().ToString() : "";

                    if (ImGui.BeginCombo($"##trigger-channels{i}", allowedChannelsPreview))
                    {
                        ImGui.Text("Search");
                        ImGui.SameLine();
                        ImGui.InputText($"##trigger-channelsSearch{i}", ref Search, 100);

                        var filteredChannels = Enum.GetValues<XivChatType>().OrderBy(chatType => chatType.ToString()).Where(chatType => chatType.ToString().Contains(Search, StringComparison.OrdinalIgnoreCase));
                        foreach (var channelType in filteredChannels) 
                        {
                            if(ImGui.Selectable(channelType.ToString(), trigger.AllowedChannels.Contains(channelType)))
                            {
                                if (trigger.AllowedChannels.Contains(channelType)) trigger.AllowedChannels.Remove(channelType);
                                else trigger.AllowedChannels.Add(channelType);
                                Configuration.Save();
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.TableNextColumn();

                    bool enabled = trigger.Enabled;
                    if(ImGui.Checkbox($"##trigger-enabled{i}", ref enabled))
                    {
                        trigger.Enabled = enabled;
                        Configuration.Save();
                    }

                    ImGui.TableNextColumn();

                    if (ImGui.Button($"Delete##trigger-delete{i}"))
                    {
                        Configuration.Triggers.RemoveAt(i);
                        Configuration.Save();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                }

            }
            ImGui.EndTable();

            ImGui.EndTabItem();
        }
    }
}
