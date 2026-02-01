using System.Collections.Concurrent;

namespace Com.Poker.Core
{
    /// <summary>
    /// Global shared runtime data.
    /// No GameObject required.
    /// Thread-safe where applicable.
    /// </summary>
    public static class GlobalSharedData
    {
        // =========================
        // PLAYER SESSION
        // =========================
        public static string MyPlayerID { get; set; } = string.Empty;
        public static string MyLaunchToken { get; set; } = string.Empty;
        public static string MyWebsocketUrl { get; set; } = string.Empty;
        public static string MyOperatorGameID { get; set; } = string.Empty;

        // =========================
        // TABLE SELECTION
        // =========================
        public static string MySelectedTableCode { get; set; } = string.Empty;
        public static int MySelectedMatchSizeID { get; set; } = 0;
        public static string PendingMatchSizeTableCode { get; set; } = string.Empty;

        // =========================
        // TABLE DATA CACHE
        // Thread-safe for network callbacks
        // =========================
        //public static readonly ConcurrentDictionary<string, PokerTableInfo> PokerTables
        //    = new ConcurrentDictionary<string, PokerTableInfo>();

        // =========================
        // UTILITIES
        // =========================
        public static void ResetSession()
        {
            MyPlayerID = string.Empty;
            MyLaunchToken = string.Empty;
            MySelectedTableCode = string.Empty;
            MySelectedMatchSizeID = 0;
            PendingMatchSizeTableCode = string.Empty;
            MyWebsocketUrl = string.Empty;
            MyOperatorGameID = string.Empty;

            //PokerTables.Clear();
        }
    }
}

