using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Reaction;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

internal static class ReactionWheelAspectRatioPatch
{
    private static readonly FieldInfo? CenterPositionField = AccessTools.Field(typeof(NReactionWheel), "_centerPosition");
    private static readonly FieldInfo? SelectedWedgeField = AccessTools.Field(typeof(NReactionWheel), "_selectedWedge");
    private static readonly FieldInfo? ReactionSynchronizerContainerField = AccessTools.Field(typeof(ReactionSynchronizer), "_container");
    private static readonly Vector2 NetworkPositionHalfRange = NGame.devResolution * 0.5f;

    [HarmonyPatch(typeof(NReactionWheel), "WarpMouseBackToOriginalPosition")]
    [HarmonyPrefix]
    private static bool WarpMouseBackToOriginalPositionPrefix(NReactionWheel __instance)
    {
        if (!TryGetViewportCenterPosition(__instance, out Vector2 centerPosition))
        {
            return true;
        }

        __instance.GetViewport().WarpMouse(centerPosition);
        return false;
    }

    [HarmonyPatch(typeof(NReactionWheel), nameof(NReactionWheel._Input))]
    [HarmonyPostfix]
    private static void InputPostfix(NReactionWheel __instance, InputEvent inputEvent)
    {
        if (!__instance.Visible || !TryGetViewportCenterPosition(__instance, out Vector2 centerPosition))
        {
            return;
        }

        Vector2 canvasCenterPosition = ViewportToCanvasPosition(__instance, centerPosition);
        __instance.GlobalPosition = canvasCenterPosition - __instance.Size * __instance.Scale * 0.5f;
    }

    [HarmonyPatch(typeof(NReactionWheel), "MoveMarker")]
    [HarmonyPrefix]
    private static void MoveMarkerPrefix(NReactionWheel __instance, ref Vector2 relative)
    {
        relative = ViewportDeltaToCanvasDelta(__instance, relative);
    }

    [HarmonyPatch(typeof(NReactionWheel), "React")]
    [HarmonyPrefix]
    private static bool ReactPrefix(NReactionWheel __instance)
    {
        if (SelectedWedgeField is null || !TryGetViewportCenterPosition(__instance, out Vector2 centerPosition))
        {
            return true;
        }

        if (SelectedWedgeField.GetValue(__instance) is not NReactionWheelWedge selectedWedge)
        {
            return false;
        }

        NGame? game = NGame.Instance;
        if (game is null)
        {
            return true;
        }

        Vector2 canvasCenterPosition = ViewportToCanvasPosition(__instance, centerPosition);
        game.ReactionContainer.DoLocalReaction(selectedWedge.Reaction, canvasCenterPosition);
        return false;
    }

    [HarmonyPatch(typeof(ReactionSynchronizer), nameof(ReactionSynchronizer.SendLocalReaction))]
    [HarmonyPrefix]
    private static bool SendLocalReactionPrefix(ReactionSynchronizer __instance, ReactionType type, Vector2 mouseScreenPos)
    {
        if (ReactionSynchronizerContainerField?.GetValue(__instance) is not NReactionContainer container)
        {
            return true;
        }

        ReactionMessage message = new()
        {
            type = type,
            normalizedPosition = GetNormalizedControlPosition(mouseScreenPos, container)
        };
        __instance.NetService.SendMessage(message);
        return false;
    }

    [HarmonyPatch(typeof(ReactionSynchronizer), "HandleReactionMessage")]
    [HarmonyPrefix]
    private static bool HandleReactionMessagePrefix(ReactionSynchronizer __instance, ReactionMessage message, ulong senderId)
    {
        if (ReactionSynchronizerContainerField?.GetValue(__instance) is not NReactionContainer container)
        {
            return true;
        }

        Vector2 controlPosition = GetControlPositionFromNormalized(message.normalizedPosition, container);
        Vector2 canvasPosition = ControlToCanvasPosition(container, controlPosition);
        container.DoRemoteReaction(message.type, canvasPosition);
        return false;
    }

    private static bool TryGetViewportCenterPosition(NReactionWheel reactionWheel, out Vector2 centerPosition)
    {
        if (CenterPositionField?.GetValue(reactionWheel) is Vector2 value)
        {
            centerPosition = value;
            return true;
        }

        centerPosition = Vector2.Zero;
        return false;
    }

    private static Vector2 ViewportToCanvasPosition(CanvasItem canvasItem, Vector2 viewportPosition)
    {
        return canvasItem.GetCanvasTransform().AffineInverse() * viewportPosition;
    }

    private static Vector2 ViewportDeltaToCanvasDelta(CanvasItem canvasItem, Vector2 viewportDelta)
    {
        Transform2D viewportToCanvas = canvasItem.GetCanvasTransform().AffineInverse();
        return viewportToCanvas * viewportDelta - viewportToCanvas * Vector2.Zero;
    }

    private static Vector2 GetNormalizedControlPosition(Vector2 canvasPosition, Control rootNode)
    {
        Vector2 controlPosition = CanvasToControlPosition(rootNode, canvasPosition);
        return (controlPosition - rootNode.Size * 0.5f) / NetworkPositionHalfRange;
    }

    private static Vector2 GetControlPositionFromNormalized(Vector2 normalizedPosition, Control rootNode)
    {
        return normalizedPosition * NetworkPositionHalfRange + rootNode.Size * 0.5f;
    }

    private static Vector2 CanvasToControlPosition(Control control, Vector2 canvasPosition)
    {
        return control.GetGlobalTransform().AffineInverse() * canvasPosition;
    }

    private static Vector2 ControlToCanvasPosition(Control control, Vector2 controlPosition)
    {
        return control.GetGlobalTransform() * controlPosition;
    }
}
