////////////////////
//       RECK       //
////////////////////

namespace WCC.Core
{
    /// <summary>
    /// ⚠ WARNING:
    /// This class is a shared core vault used by the Poker system.
    /// 
    /// DO NOT modify this file unless you are explicitly authorized.
    /// Unauthorized changes may cause critical system failures,
    /// server desync issues, or security vulnerabilities.
    /// 
    /// Author: RECK
    /// </summary>
    public static class PokerSharedVault
    {
        /// <summary>
        /// Token provided during game launch authentication.
        /// </summary>
        public static string LaunchToken;

        /// <summary>
        /// Backend server base URL.
        /// </summary>
        public static string ServerURL;

        /// <summary>
        /// Operator identifier.
        /// </summary>
        public static string OperatorPublicID;

        /// <summary>
        /// Unique table session code.
        /// </summary>
        public static string TableCode;

        /// <summary>
        /// Match size configuration ID.
        /// </summary>
        public static int MatchSizeId = -1;
    }
}
