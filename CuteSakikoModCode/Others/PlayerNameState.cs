using STS2RitsuLib.RunData;

namespace CuteSakikoMod.CuteSakikoModCode.Others
{
    public sealed class PlayerNameState
    {
        public string CustomName { get; set; } = string.Empty;
    }

    public static class PlayerNameData
    {
        public static PlayerRunSavedData<PlayerNameState> PlayerNameSlot = null!;

        public static void Init(RunSavedDataStore store)
        {
            PlayerNameSlot = store.RegisterPerPlayer(
                key: "player_name",
                defaultFactory: () => new PlayerNameState(),
                options: new RunSavedDataOptions
                {
                    WritePolicy = RunSavedDataWritePolicy.WhenSet,
                    SyncLobbyOnChange = true,
                });
        }
    }
}