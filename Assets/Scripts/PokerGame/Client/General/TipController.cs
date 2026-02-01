////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class TipController : MonoBehaviour
    {
        [SerializeField] TMP_InputField _tipIF;
        [SerializeField] Button _confirmButton;

        long _maxStack;

        private void Awake() => PokerNetConnect.OnMessageEvent += OnMessage;

        private void Start()
        {
            _confirmButton.onClick.AddListener(OnClickSendTipButton);
            _tipIF.onValueChanged.AddListener(OnChangeListener);
        }

        void OnTableSnapshot(TableSnapshot m)
        {
            foreach (var p in m.Players)
            {
                if (p.PlayerId == PokerNetConnect.OwnerPlayerID && _maxStack != p.Stack)
                    _maxStack = p.Stack;
            }
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            if (type == MsgType.TableSnapshot)
            {
                OnTableSnapshot((TableSnapshot)msg);
            }
        }

        void OnClickSendTipButton()
        {
            NetworkDebugLogger.LogSend("Tip", $"tableId={GameServerClient.Instance.TableId} amount={_tipIF.text}");
            GameServerClient.SendTipStatic(GameServerClient.Instance.TableId, int.Parse(_tipIF.text));
        }

        void OnChangeListener(string change)
        {
            _confirmButton.interactable = change != string.Empty;
        }

        void OnEnable() => GameServerClient.TipResponseReceivedStatic += OnTipResponse;

        void OnDisable() => GameServerClient.TipResponseReceivedStatic -= OnTipResponse;

        void OnTipResponse(TipResponse resp)
        {
            if (resp.Success)
                Debug.Log($"Tip ok amount={resp.Amount} newStack={resp.Stack}"); //--eto na yong udpated balance mo after ka mag bigay ng tip. Make an animation na merong chips na papunta sa Girl na nasa table
            else
                Debug.Log($"Tip failed reason={resp.Message}");
        }
    }
}
