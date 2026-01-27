using System;
using Com.poker.Core;
using Google.Protobuf;
using UnityEngine;

// PokerBotSpectatorDataDump
// -------------------------
// This script listens to ALL server messages and prints the data to the Console.
// No UI rendering here. It's meant as a reference for junior devs to see
// what data is available for each message type.
public sealed class PokerBotSpectatorDataDump : MonoBehaviour
{
    [Header("Auto Connect")]
    public bool autoConnectOnStart = true;
    public string serverUrl = "ws://51.79.160.227:26001/ws";
    public string launchToken = "";
    public string operatorPublicId = "cde5ce9f-8df7-f011-ace1-b2b431609323";
    public bool autoSpectateOnConnect = true;
    public string spectateTableCode = "DEV-BOT-TABLE";

    void Start()
    {
        if (!autoConnectOnStart)
            return;

        if (string.IsNullOrWhiteSpace(serverUrl) ||
            string.IsNullOrWhiteSpace(launchToken) ||
            string.IsNullOrWhiteSpace(operatorPublicId))
        {
            Debug.LogWarning("[BotDump] Missing serverUrl/launchToken/operatorPublicId. Auto-connect skipped.");
            return;
        }

        GameServerClient.Configure(serverUrl);
        GameServerClient.ConnectWithLaunchToken(launchToken, operatorPublicId);
    }

    void OnEnable()
    {
        GameServerClient.MessageReceivedStatic += OnMessage;
        GameServerClient.ConnectResponseReceivedStatic += OnConnect;
    }

    void OnDisable()
    {
        GameServerClient.MessageReceivedStatic -= OnMessage;
        GameServerClient.ConnectResponseReceivedStatic -= OnConnect;
    }

    void OnConnect(ConnectResponse resp)
    {
        if (!autoSpectateOnConnect)
            return;

        if (string.IsNullOrWhiteSpace(spectateTableCode))
        {
            Debug.LogWarning("[BotDump] spectateTableCode is empty.");
            return;
        }

        GameServerClient.SendSpectateStatic(spectateTableCode);
    }

    void OnMessage(MsgType type, IMessage msg)
    {
        switch (type)
        {
            case MsgType.ConnectResponse:
            {
                // Connection accepted by server.
                var m = (ConnectResponse)msg;
                // Data you can use:
                // - m.PlayerId   : unique player id
                // - m.SessionId  : websocket session id
                // - m.Credits    : player credits from operator
                // - m.ProtocolVersion
                Debug.Log($"[Connect] playerId={m.PlayerId} sessionId={m.SessionId} credits={m.Credits}");
                break;
            }
            case MsgType.TableList:
            {
                // List of available poker tables (from operator).
                var m = (PokerTableList)msg;
                // Data you can use per table:
                // - TableCode, TableName
                // - SmallBlind, BigBlind
                // - MinBuyIn, MaxBuyIn
                // - MaxPlayers, MinPlayersToStart
                Debug.Log($"[TableList] count={m.Tables.Count}");
                foreach (var t in m.Tables)
                {
                    Debug.Log($"  table={t.TableCode} name={t.TableName} sb={t.SmallBlind} bb={t.BigBlind} maxPlayers={t.MaxPlayers}");
                }
                break;
            }
            case MsgType.TableSnapshot:
            {
                // Full table state (seats, stacks, pot, board).
                var m = (TableSnapshot)msg;
                // Data you can use:
                // - m.State (waiting/preflop/flop/turn/river/showdown/reset)
                // - m.CommunityCards (board cards if already revealed)
                // - m.PotTotal, m.CurrentBet, m.MinRaise
                // - m.CurrentTurnSeat
                Debug.Log($"[Snapshot] table={m.TableId} state={m.State} pot={m.PotTotal} currentBet={m.CurrentBet}");
                foreach (var p in m.Players)
                {
                    Debug.Log($"  seat={p.Seat} player={p.PlayerId} stack={p.Stack} bet={p.BetThisRound} status={p.Status}");
                }
                // Community cards (if any are already revealed)
                // You can read them like:
                // - m.CommunityCards[0] -> first board card
                // - m.CommunityCards[1] -> second board card
                // If no board yet (pre-flop), count = 0.
                if (m.CommunityCards != null && m.CommunityCards.Count > 0)
                    Debug.Log($"  board={FormatCards(m.CommunityCards)}");
                break;
            }
            case MsgType.DealHoleCards:
            {
                // Private hole cards for THIS player only.
                var m = (DealHoleCards)msg;
                // Data you can use:
                // - m.Cards[0..1] : the two hole cards
                // How to read a card:
                // - card.Rank (2-14, where 14 = Ace)
                // - card.Suit (0=Clubs,1=Diamonds,2=Hearts,3=Spades)
                // Example:
                //   var c1 = m.Cards[0];
                //   var c2 = m.Cards[1];
                //   Debug.Log($"card1 rank={c1.Rank} suit={c1.Suit}");
                Debug.Log($"[HoleCards] cards={FormatCards(m.Cards)}");
                break;
            }
            case MsgType.SpectatorHoleCards:
            {
                // Spectator-only hole cards (DEV MODE ONLY).
                // This is NOT sent to real players.
                // It is used for testing/verification so spectators can see all hole cards.
                var m = (SpectatorHoleCards)msg;
                // Data you can use:
                // - m.PlayerId : whose cards
                // - m.Cards    : that player's 2 hole cards
                Debug.Log($"[SpectatorHoleCards] player={m.PlayerId} cards={FormatCards(m.Cards)}");
                break;
            }
            case MsgType.CommunityCards:
            {
                // Board cards for flop/turn/river.
                var m = (CommunityCards)msg;
                // Data you can use:
                // - m.Street (Flop/Turn/River)
                // - m.Cards (cards revealed for that street)
                // How to read cards:
                // - m.Cards[0] = first card in this street
                // - m.Cards[1] = second card in this street, etc
                // Each card has Rank/Suit like in DealHoleCards.
                Debug.Log($"[Board] street={m.Street} cards={FormatCards(m.Cards)}");
                break;
            }
            case MsgType.TurnUpdate:
            {
                // Whose turn + allowed actions.
                var m = (TurnUpdate)msg;
                // Data you can use:
                // - m.PlayerId, m.Seat (current turn)
                // - Seat number starts at 1 (not 0)
                // - m.AllowedActions (Fold/Check/Call/Bet/Raise/AllIn)
                // - m.CallAmount, m.MinRaise, m.MaxRaise
                // - m.Stack (current player stack)
                // - m.DeadlineUnixMs (absolute server time when turn expires)
                //   Use this to build a countdown/progress bar:
                //   remainingMs = max(0, DeadlineUnixMs - nowUtcMs)
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var remainingMs = m.DeadlineUnixMs > 0 ? Math.Max(0, (long)m.DeadlineUnixMs - nowMs) : 0;
                Debug.Log($"[Turn] player={m.PlayerId} seat={m.Seat} allowed={m.AllowedActions.Count} remainingMs={remainingMs}");
                break;
            }
            case MsgType.ActionRequest:
            {
                // Server asking YOU to act (if you're a player).
                var m = (ActionRequest)msg;
                // Data you can use:
                // - m.Action (type)
                // - m.Amount (suggested amount)
                // You can also use TurnUpdate.AllowedActions to know what buttons to enable.
                // ActionRequest is usually used together with TurnUpdate.
                Debug.Log($"[ActionRequest] action={m.Action} amount={m.Amount}");
                break;
            }
            case MsgType.ActionResult:
            {
                // Result of YOUR action (success/fail).
                var m = (ActionResult)msg;
                // Data you can use:
                // - m.Success, m.Message
                // - m.Action, m.Amount
                // - m.CurrentBet, m.MinRaise
                // - m.Stack (your updated stack)
                Debug.Log($"[ActionResult] ok={m.Success} action={m.Action} amount={m.Amount} stack={m.Stack}");
                break;
            }
            case MsgType.ActionBroadcast:
            {
                // Broadcast of OTHER players' actions.
                var m = (ActionBroadcast)msg;
                // Data you can use:
                // - m.PlayerId
                // - m.Action, m.Amount
                // - m.CurrentBet, m.PotTotal
                Debug.Log($"[ActionBroadcast] player={m.PlayerId} action={m.Action} amount={m.Amount} pot={m.PotTotal}");
                break;
            }
            case MsgType.PotUpdate:
            {
                // Pot and side pots.
                var m = (PotUpdate)msg;
                // Data you can use:
                // - m.PotTotal
                // - m.Pots (each side pot amount + eligible players)
                Debug.Log($"[PotUpdate] total={m.PotTotal} sidePots={m.Pots.Count}");
                break;
            }
            case MsgType.StackUpdate:
            {
                // Single player's stack update.
                var m = (StackUpdate)msg;
                // Data you can use:
                // - m.PlayerId
                // - m.Stack (stack = player's current chips in this match)
                Debug.Log($"[StackUpdate] player={m.PlayerId} stack={m.Stack}");
                break;
            }
            case MsgType.HandResult:
            {
                // Hand finished. Winners + revealed hands.
                var m = (HandResult)msg;
                // Data you can use:
                // - m.Winners (playerId, amount, rank, bestFive)
                // - m.RevealedHands (playerId + hole cards)
                Debug.Log($"[HandResult] winners={m.Winners.Count} revealed={m.RevealedHands.Count}");
                break;
            }
            case MsgType.JoinTableResponse:
            {
                // Join table response (seat, blinds).
                var m = (JoinTableResponse)msg;
                Debug.Log($"[JoinTable] ok={m.Success} table={m.TableId} seat={m.Seat}");
                break;
            }
            case MsgType.LeaveTableResponse:
            {
                // Leave table response.
                var m = (LeaveTableResponse)msg;
                Debug.Log($"[LeaveTable] ok={m.Success} msg={m.Message}");
                break;
            }
            case MsgType.BuyinResponse:
            {
                // Buy-in response (wallet -> game stack).
                var m = (BuyInResponse)msg;
                // Data you can use:
                // - m.Amount (requested buy-in amount)
                // - m.Balance (operator wallet balance after buy-in)
                Debug.Log($"[BuyIn] ok={m.Success} amount={m.Amount} balance={m.Balance} msg={m.Message}");
                break;
            }
            case MsgType.SpectateResponse:
            {
                // Spectator response (ok or not).
                var m = (SpectateResponse)msg;
                Debug.Log($"[Spectate] ok={m.Success} table={m.TableId} msg={m.Message}");
                break;
            }
            case MsgType.Kick:
            {
                // Server kicked the client.
                var m = (Kick)msg;
                Debug.LogWarning($"[Kick] reason={m.Reason}");
                break;
            }
            case MsgType.Error:
            {
                // Server error message.
                var m = (Com.poker.Core.Error)msg;
                Debug.LogWarning($"[Error] code={m.Code} msg={m.Message}");
                break;
            }
        }
    }

    static string FormatCards(Google.Protobuf.Collections.RepeatedField<Card> cards)
    {
        // Converts list of Card -> readable string like "A_H 10_S 2_C"
        if (cards == null || cards.Count == 0) return "-";
        var parts = new string[cards.Count];
        for (int i = 0; i < cards.Count; i++)
            parts[i] = FormatCard(cards[i]);
        return string.Join(" ", parts);
    }

    static string FormatCard(Card c)
    {
        // Converts one Card to readable name using our sprite naming
        // Example: Rank=14 Suit=2 -> "A_H"
        string r = c.Rank switch
        {
            14 => "A", 13 => "K", 12 => "Q", 11 => "J",
            10 => "10", _ => c.Rank.ToString()
        };
        string s = c.Suit switch { 0 => "C", 1 => "D", 2 => "H", 3 => "S", _ => "?" };
        return r + "_" + s;
    }
}
