using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.addons.mega_text;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class NameChangeCmd
    {
        private const int MaxNameLength = 30;

        public static async Task ShowRenameDialog(Player targetPlayer)
        {
            var targetName = targetPlayer.Character.Title.GetFormattedText();

            var titleLoc = new LocString("cards", "CUTE_SAKIKO_MOD_CARD_NAME_SENSE.dialog.title");
            titleLoc.Add("target", targetName);
            var title = titleLoc.GetFormattedText();

            var placeholderLoc = new LocString("cards", "CUTE_SAKIKO_MOD_CARD_NAME_SENSE.dialog.placeholder");
            placeholderLoc.Add("target", targetName);
            var placeholder = placeholderLoc.GetFormattedText();

            var confirmLoc = new LocString("cards", "CUTE_SAKIKO_MOD_CARD_NAME_SENSE.dialog.confirm");
            var confirmText = confirmLoc.GetFormattedText();

            var cancelLoc = new LocString("cards", "CUTE_SAKIKO_MOD_CARD_NAME_SENSE.dialog.cancel");
            var cancelText = cancelLoc.GetFormattedText();

            var dialog = new AcceptDialog
            {
                Title = title,
                Size = new Vector2I(400, 200),
                DialogText = " ",
            };

            var lineEdit = new LineEdit
            {
                PlaceholderText = placeholder,
                Size = new Vector2I(300, 30),
                Position = new Vector2I(50, 50),
                MaxLength = MaxNameLength,
            };
            dialog.AddChild(lineEdit);

            var okButton = dialog.GetOkButton();
            if (okButton != null)
                okButton.Text = confirmText;

            var cancelButton = dialog.AddCancelButton(cancelText);

            var tcs = new TaskCompletionSource<string>();
            dialog.Confirmed += () =>
            {
                var rawName = lineEdit.Text.Trim();
                var safeName = SanitizePlayerName(rawName);
                tcs.SetResult(safeName);
            };
            dialog.Canceled += () => tcs.SetResult(null);

            Node parent = NGame.Instance ?? (Node)NRun.Instance?.GlobalUi;
            if (parent == null)
                return;

            parent.AddChildSafely(dialog);
            dialog.PopupCentered();

            var newName = await tcs.Task;
            dialog.QueueFree();

            if (!string.IsNullOrEmpty(newName))
            {
                // 1. 本地存储
                var runState = GetCurrentRunState();
                if (runState != null)
                {
                    PlayerNameData.PlayerNameSlot.Modify(runState, targetPlayer.NetId, data =>
                    {
                        data.CustomName = newName;
                    });
                }

                // 2. 立即刷新 UI
                RefreshAllPlayerNameUI();

                // 3. 网络广播
                var netService = RunManager.Instance?.NetService;
                if (netService != null && netService.Type != NetGameType.Singleplayer)
                {
                    netService.SendMessage<NameChangeMessage>(new NameChangeMessage
                    {
                        TargetNetId = targetPlayer.NetId,
                        NewName = newName
                    });
                }
            }
        }

        public static void RefreshAllPlayerNameUI()
        {
            var nrun = NRun.Instance;
            if (nrun == null) return;

            var platform = RunManager.Instance.NetService.Platform;

            // 1. 左上角玩家列表
            RefreshMultiplayerPlayerStates(nrun, platform);

            // 2. 战斗中玩家模型下方名字
            RefreshCombatPlayerNames(nrun, platform);

            // 3. 大厅远程玩家列表（如果存在）
            RefreshLobbyPlayers(nrun, platform);
        }

        private static void RefreshMultiplayerPlayerStates(NRun nrun, PlatformType platform)
        {
            var container = nrun.GlobalUi?.MultiplayerPlayerContainer;
            if (container == null) return;

            // 直接遍历子节点（均为 NMultiplayerPlayerState）
            var children = container.GetChildren();
            foreach (var child in children)
            {
                if (child is NMultiplayerPlayerState playerState && playerState.Player != null)
                {
                    // 调用会被补丁拦截的原始方法，自动返回自定义名字
                    string newName = PlatformUtil.GetPlayerNameRaw(platform, playerState.Player.NetId);

                    // 反射获取 _nameplateLabel 并调用 SetTextAutoSize
                    var labelField = typeof(NMultiplayerPlayerState).GetField("_nameplateLabel",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (labelField?.GetValue(playerState) is MegaLabel label)
                    {
                        label.SetTextAutoSize(newName);
                    }
                }
            }

            // 确保容器可见并刷新
            container.ShowImmediately();
            container.QueueRedraw();
        }

        private static void RefreshCombatPlayerNames(NRun nrun, PlatformType platform)
        {
            var combatRoom = nrun.CombatRoom;
            if (combatRoom == null) return;

            foreach (var creatureNode in combatRoom.CreatureNodes)
            {
                var creature = creatureNode.Entity;
                if (creature == null || !creature.IsPlayer || creature.Player == null) continue;

                var stateDisplayField = typeof(NCreature).GetField("_stateDisplay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateDisplayField == null) continue;

                var stateDisplay = stateDisplayField.GetValue(creatureNode);
                if (stateDisplay == null) continue;

                var nameLabelField = stateDisplay.GetType().GetField("_nameplateLabel",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameLabelField?.GetValue(stateDisplay) is MegaLabel nameLabel)
                {
                    string newName = PlatformUtil.GetPlayerNameRaw(platform, creature.Player.NetId);
                    nameLabel.SetTextAutoSize(newName);
                }
            }
        }

        private static void RefreshLobbyPlayers(NRun nrun, PlatformType platform)
        {
            var container = nrun.GlobalUi?.GetNodeOrNull<Control>("RemoteLobbyPlayerContainer");
            if (container == null) return;

            foreach (Node child in container.GetChildren())
            {
                if (child is NRemoteLobbyPlayer playerNode)
                {
                    var method = typeof(NRemoteLobbyPlayer).GetMethod("RefreshVisuals",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    method?.Invoke(playerNode, null);
                }
            }
        }

        private static string SanitizePlayerName(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"\[\/?[a-zA-Z0-9_\-=#]+\]", "");
            sanitized = new string(sanitized.Where(c => c >= 32 && c != '[' && c != ']' && c != '<' && c != '>').ToArray());
            if (sanitized.Length > MaxNameLength)
                sanitized = sanitized[..MaxNameLength];
            return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized.Trim();
        }

        public static RunState? GetCurrentRunState()
        {
            var nrun = NRun.Instance;
            if (nrun == null) return null;
            var field = typeof(NRun).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(nrun) as RunState;
        }
    }
}