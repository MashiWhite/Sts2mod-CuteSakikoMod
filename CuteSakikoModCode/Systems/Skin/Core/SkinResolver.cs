using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;

public static class SkinResolver
{
    private static readonly Dictionary<Type, CharacterSkinChoice> _localCache = [];
    private static readonly Dictionary<ulong, Dictionary<Type, CharacterSkinChoice>> _remoteCache = [];

    public static ulong LocalNetId { get; private set; }

    public static void SetLocalNetId(ulong netId)
    {
        LocalNetId = netId;
    }

    public static CharacterSkinChoice GetLocalChoice(Type characterType)
    {
        return _localCache.TryGetValue(characterType, out var c) ? c : new CharacterSkinChoice();
    }

    /// <summary>取任意玩家（本机或远端缓存）的完整皮肤/预设选择。</summary>
    public static CharacterSkinChoice GetChoiceForPlayer(ulong netId, Type characterType)
    {
        if (netId == LocalNetId)
            return GetLocalChoice(characterType);

        if (_remoteCache.TryGetValue(netId, out var perChar)
            && perChar.TryGetValue(characterType, out var choice))
            return choice.Clone();

        return new CharacterSkinChoice();
    }

    public static CharacterSkinDefinition? GetLocalSkin(Type characterType)
    {
        return CharacterSkinRegistry.GetSkin(characterType, GetLocalChoice(characterType).ArtSkinIndex);
    }

    public static void SetLocalChoice(Type characterType, CharacterSkinChoice choice)
    {
        _localCache[characterType] = choice;
    }

    public static void ModifyLocalChoice(Type characterType, Action<CharacterSkinChoice> mutator)
    {
        if (!_localCache.TryGetValue(characterType, out var choice))
            _localCache[characterType] = choice = new CharacterSkinChoice();
        mutator(choice);
    }

    public static bool HasRemote(ulong netId, Type characterType)
    {
        return _remoteCache.TryGetValue(netId, out var m) && m.ContainsKey(characterType);
    }

    public static CharacterSkinDefinition? GetSkinForPlayer(ulong netId, Type characterType)
    {
        if (netId == LocalNetId)
            return GetLocalSkin(characterType);

        if (_remoteCache.TryGetValue(netId, out var perChar)
            && perChar.TryGetValue(characterType, out var choice))
            return CharacterSkinRegistry.GetSkin(characterType, choice.ArtSkinIndex);

        return CharacterSkinRegistry.GetSkin(characterType, 0);
    }

    public static void CacheRemote(ulong netId, Type characterType, CharacterSkinChoice choice)
    {
        if (!_remoteCache.TryGetValue(netId, out var perChar))
            _remoteCache[netId] = perChar = [];
        perChar[characterType] = choice;
    }

    public static void ClearRemoteCache()
    {
        _remoteCache.Clear();
    }

    /// <summary>
    ///     跑局开始时遍历所有玩家。★ 强制覆盖本机缓存——因为客机 join 存档时不会走大厅，
    ///     唯一的本机皮肤来源就是 run snapshot。
    /// </summary>
    public static void SyncFromRun(RunState run, ulong localNetId)
    {
        SetLocalNetId(localNetId);
        ClearRemoteCache();

        GD.Print($"[SkinResolver] SyncFromRun: localNetId={localNetId}, players={run.Players.Count}");

        foreach (var player in run.Players)
        {
            var netId = player.NetId;
            foreach (var registry in CharacterSkinRegistry.GetAll())
            {
                var choice = SkinDataStore.GetRunChoice(player, registry.CharacterType);
                var isLocal = netId == localNetId;

                GD.Print($"[SkinResolver]   player={netId} char={registry.CharacterType.Name} " +
                         $"art={choice.ArtSkinIndex} isLocal={isLocal}");

                if (isLocal)
                    _localCache[registry.CharacterType] = choice.Clone(); // ★ 强制覆盖
                else
                    CacheRemote(netId, registry.CharacterType, choice);
            }
        }
    }

    /// <summary>
    ///     从大厅数据同步（用于角色选择界面）。
    ///     forceResetLocal = true：进入角色选择界面时调用，清空并重建本机缓存。
    ///     forceResetLocal = false：事件回调时调用，只更新远端缓存。
    /// </summary>
    public static void SyncFromLobby(StartRunLobby lobby, bool forceResetLocal = false)
    {
        if (lobby == null) return;

        var localId = lobby.NetService.NetId;
        SetLocalNetId(localId);

        _remoteCache.Clear();

        if (forceResetLocal)
            _localCache.Clear();

        foreach (var p in lobby.Players)
        {
            if (p.character == null) continue;
            var charType = p.character.GetType();
            if (!CharacterSkinRegistry.HasSkins(charType)) continue;

            var hasData = SkinDataStore.HasLobbyChoice(lobby, p.id, charType);
            var choice = hasData
                ? SkinDataStore.GetLobbyChoice(lobby, p.id, charType)
                : new CharacterSkinChoice();

            if (p.id == localId)
            {
                if (forceResetLocal || !_localCache.ContainsKey(charType))
                    _localCache[charType] = choice.Clone();
            }
            else
            {
                if (hasData)
                    CacheRemote(p.id, charType, choice);
            }
        }
    }

    public static Type? GetCurrentCharacterTypeFromLobby(StartRunLobby? lobby)
    {
        return lobby?.LocalPlayer.character?.GetType();
    }

    public static IReadOnlyList<ICharacterSkinRegistry> GetAllRegistries()
    {
        return CharacterSkinRegistry.GetAll();
    }
}