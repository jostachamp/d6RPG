using System;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Scenes
{
    public enum SceneKind
    {
        Narrative = 0,
        Structured = 1
    }

    public enum SceneSide
    {
        A = 0,
        B = 1
    }

    public enum SceneActionKind
    {
        Free = 0,
        Simple = 1,
        Complex = 2,
        Reaction = 3
    }

    public enum SimpleActionKind
    {
        General = 0,
        AidAlly = 1
    }

    public sealed class SceneActor
    {
        public SceneActor(string id, SceneSide side, DicePool dicePool)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An actor ID is required.", nameof(id));
            }

            Id = id;
            Side = side;
            DicePool = dicePool ?? throw new ArgumentNullException(nameof(dicePool));
        }

        public string Id { get; }

        public SceneSide Side { get; }

        public DicePool DicePool { get; }
    }

    public static class SceneActionRules
    {
        public static int SimpleActionCost(SceneKind sceneKind, SimpleActionKind actionKind)
        {
            Validate(sceneKind, actionKind);
            return sceneKind == SceneKind.Structured || actionKind == SimpleActionKind.AidAlly ? 1 : 0;
        }

        private static void Validate(SceneKind sceneKind, SimpleActionKind actionKind)
        {
            if (!Enum.IsDefined(typeof(SceneKind), sceneKind))
            {
                throw new ArgumentOutOfRangeException(nameof(sceneKind));
            }

            if (!Enum.IsDefined(typeof(SimpleActionKind), actionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(actionKind));
            }
        }
    }
}
