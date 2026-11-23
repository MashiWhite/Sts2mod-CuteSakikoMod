using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;
using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Networking;

public static class SkinSyncService
{
    private static bool _registered;

    public static void Init()
    {
        if (_registered) return;
        _registered = true;
    }

    public static void Broadcast(Type characterType)
    {
        if (RunManager.Instance == null) return;
        var net = RunManager.Instance.NetService;
        if (net == null || !net.IsConnected) return;
        if (net.Type == NetGameType.Singleplayer || net.Type == NetGameType.None) return;

        var choice = SkinResolver.GetLocalChoice(characterType);
        var msg = new SkinSyncMessage
        {
            SourceNetId = net.NetId,
            CharacterTypeName = characterType.FullName ?? characterType.Name,
            ArtSkinIndex = choice.ArtSkinIndex,
            DeckPresetIndex = choice.DeckPresetIndex,
            RelicPresetIndex = choice.RelicPresetIndex,
            MutsumiArchetypeIndex = choice.MutsumiArchetypeIndex
        };

        if (net.Type == NetGameType.Client)
            net.SendMessage(msg, 0UL);
        else
            net.SendMessage(msg);

        GD.Print($"[SkinSync] Broadcast: {characterType.Name} art={choice.ArtSkinIndex}");
    }

    public static void OnMessageReceived(SkinSyncMessage msg, ulong senderNetId)
    {
        var type = ResolveType(msg.CharacterTypeName);
        if (type == null)
        {
            GD.PrintErr($"[SkinSync] Unknown character type: {msg.CharacterTypeName}");
            return;
        }

        SkinResolver.CacheRemote(msg.SourceNetId, type, new CharacterSkinChoice
        {
            ArtSkinIndex = msg.ArtSkinIndex,
            DeckPresetIndex = msg.DeckPresetIndex,
            RelicPresetIndex = msg.RelicPresetIndex,
            MutsumiArchetypeIndex = msg.MutsumiArchetypeIndex
        });

        SkinSystemEvents.RaiseArtSkinChanged(type);
    }

    private static Type? ResolveType(string fullName)
    {
        foreach (var reg in CharacterSkinRegistry.GetAll())
        {
            var t = reg.CharacterType;
            if ((t.FullName ?? t.Name) == fullName) return t;
        }

        return null;
    }

    // SkinSyncService 里加
    public static void PrimeRemoteFromLobby(StartRunLobby lobby)
    {
        if (lobby == null) return;
        var localId = lobby.NetService.NetId;
        foreach (var p in lobby.Players)
        {
            if (p.id == localId) continue; // 跳过本机
            if (p.character == null) continue;
            var charType = p.character.GetType();
            if (!CharacterSkinRegistry.HasSkins(charType)) continue;
            // 只在大厅阶段没有远端数据时填默认，等 SkinSyncMessage 到了再覆盖
            if (!SkinResolver.HasRemote(p.id, charType))
                SkinResolver.CacheRemote(p.id, charType, new CharacterSkinChoice());
        }
    }
}