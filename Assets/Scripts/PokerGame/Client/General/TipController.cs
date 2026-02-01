////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using DG.Tweening;
using Google.Protobuf;
using NaughtyAttributes;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WCC.Core.Audio;


namespace WCC.Poker.Client
{
    public class TipController : MonoBehaviour
    {
        [SerializeField] TMP_InputField _tipIF;
        [SerializeField] Button _confirmButton;
        [SerializeField] Button _tipButton;
        [SerializeField] GameObject _tipChipsPrefab;
        [SerializeField] Transform _instanceContainer;
        [SerializeField] Transform _startPosition;
        [SerializeField] Transform _endPosition;

        [Header("Tip Animation")]
        [SerializeField] float _fastDuration = 0.35f;
        [SerializeField] float _slowDuration = 3f;
        [SerializeField, Range(0f, 0.5f)] float _slowZoneHalf = 0.3f;
        [SerializeField] float _startScaleMultiplier = 0.3f;
        [SerializeField] float _midScaleMultiplier = 0.5f;
        [SerializeField] float _slowScaleMultiplier = 1.5f;

        [Header("[EVENTS]")]
        [SerializeField] UnityEvent _onTipRecievedEvent;

        long _maxStack;

        private void Awake() => PokerNetConnect.OnMessageEvent += OnMessage;

        private void Start()
        {
            _tipButton.interactable = false;
            _confirmButton.onClick.AddListener(OnClickSendTipButton);
            _tipIF.onValueChanged.AddListener(OnChangeListener);
        }
        void OnEnable() => GameServerClient.TipResponseReceivedStatic += OnTipResponse;

        void OnDisable() => GameServerClient.TipResponseReceivedStatic -= OnTipResponse;

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
            switch(type)
            {
                case MsgType.TableSnapshot: OnTableSnapshot((TableSnapshot)msg); break;
                case MsgType.TurnUpdate: OnTurnUpdate((TurnUpdate)msg); break;
            }
        }

        void OnClickSendTipButton()
        {
            GameServerClient.SendTipStatic(GameServerClient.Instance.TableId, int.Parse(_tipIF.text));
            print("OnClickSendTipButton");
        }

        void OnChangeListener(string change) => _confirmButton.interactable = change != string.Empty;

        void OnTipResponse(TipResponse resp)
        {
            if (resp.Success)
                Debug.Log($"Tip ok amount={resp.Amount} newStack={resp.Stack}"); //--eto na yong udpated balance mo after ka mag bigay ng tip. Make an animation na merong chips na papunta sa Girl na nasa table
            else
                Debug.Log($"Tip failed reason={resp.Message}");

            if (resp.Success)
            {
                InstantiateChips(_tipChipsPrefab, _startPosition.position, _endPosition.position, 0.8f, ins =>
                {
                    Destroy(ins);
                    AudioManager.main.PlayAudio("Chips_Pot", 0);
                    _onTipRecievedEvent?.Invoke();
                });
            }
        }

        void InstantiateChips(GameObject prefab, Vector2 startPosition, Vector2 destination, float moveDuration, [Optional] UnityAction<GameObject> isReachedCallback)
        {
            var betHolder = Instantiate(prefab, _instanceContainer);
            AudioManager.main.PlayRandomAudio("Winner", Vector2.zero);
            var t = betHolder.transform;
            var initialScale = t.localScale;

            if (GameServerClient.Instance.IsCatchingUp)
            {
                t.position = destination;
                t.localScale = initialScale;
                t.localRotation = Quaternion.Euler(0, 0, 0);
                isReachedCallback?.Invoke(betHolder);
                return;
            }

            var seq = DOTween.Sequence();

            if (betHolder.TryGetComponent<RectTransform>(out var betRect) && _instanceContainer is RectTransform containerRect)
            {
                var canvas = _instanceContainer.GetComponentInParent<Canvas>();
                var startLocal = ToLocalPoint(containerRect, canvas, startPosition);
                var endLocal = ToLocalPoint(containerRect, canvas, destination);
                var centerLocal = ScreenCenterToLocalPoint(containerRect, canvas);
                var slowDuration = _slowDuration;
                var fastDuration = Mathf.Max(0.05f, _fastDuration);
                var slowZoneHalf = Mathf.Clamp01(_slowZoneHalf);
                var slowStartLocal = Vector2.Lerp(centerLocal, startLocal, slowZoneHalf);
                var slowEndLocal = Vector2.Lerp(centerLocal, endLocal, slowZoneHalf);
                var startScale = initialScale * _startScaleMultiplier;
                var midScale = initialScale * _midScaleMultiplier;
                var slowScale = initialScale * _slowScaleMultiplier;

                betRect.anchoredPosition = startLocal;
                betRect.localScale = startScale;

                seq.Append(betRect.DOScale(midScale, fastDuration).SetEase(Ease.OutSine));
                seq.Join(DOTween.To(() => betRect.anchoredPosition, p => betRect.anchoredPosition = p, slowStartLocal, fastDuration)
                    .SetEase(Ease.OutQuad));
                seq.Append(betRect.DOScale(slowScale, slowDuration).SetEase(Ease.OutSine));
                seq.Join(DOTween.To(() => betRect.anchoredPosition, p => betRect.anchoredPosition = p, slowEndLocal, slowDuration)
                    .SetEase(Ease.Linear));
                seq.Append(betRect.DOScale(Vector3.zero, fastDuration).SetEase(Ease.InQuad));
                seq.Join(DOTween.To(() => betRect.anchoredPosition, p => betRect.anchoredPosition = p, endLocal, fastDuration)
                    .SetEase(Ease.InQuad));
            }
            else
            {
                t.position = startPosition;
                t.localScale = initialScale * 0.3f;
                var centerWorld = ScreenCenterToWorldPoint(t.position.z);
                var slowDuration = _slowDuration;
                var fastDuration = Mathf.Max(0.05f, _fastDuration);
                var slowZoneHalf = Mathf.Clamp01(_slowZoneHalf);
                var slowStartWorld = Vector3.Lerp(centerWorld, startPosition, slowZoneHalf);
                var slowEndWorld = Vector3.Lerp(centerWorld, destination, slowZoneHalf);
                var startScale = initialScale * _startScaleMultiplier;
                var midScale = initialScale * _midScaleMultiplier;
                var slowScale = initialScale * _slowScaleMultiplier;

                seq.Append(t.DOScale(midScale, fastDuration).SetEase(Ease.OutSine));
                seq.Join(t.DOMove(slowStartWorld, fastDuration).SetEase(Ease.OutQuad));
                seq.Append(t.DOScale(slowScale, slowDuration).SetEase(Ease.OutSine));
                seq.Join(t.DOMove(slowEndWorld, slowDuration).SetEase(Ease.Linear));
                seq.Append(t.DOScale(Vector3.zero, fastDuration).SetEase(Ease.InQuad));
                seq.Join(t.DOMove(destination, fastDuration).SetEase(Ease.InQuad));
            }

            seq.OnComplete(() =>
            {
                t.localRotation = Quaternion.Euler(0, 0, 0);
                isReachedCallback?.Invoke(betHolder);
            });
        }

        Vector2 ToLocalPoint(RectTransform container, Canvas canvas, Vector2 worldPosition)
        {
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container,
                RectTransformUtility.WorldToScreenPoint(cam, worldPosition),
                cam,
                out var localPoint);
            return localPoint;
        }

        Vector2 ScreenCenterToLocalPoint(RectTransform container, Canvas canvas)
        {
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container,
                new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                cam,
                out var localPoint);
            return localPoint;
        }

        Vector3 ScreenCenterToWorldPoint(float z)
        {
            var cam = Camera.main;
            if (cam == null)
                return new Vector3(0f, 0f, z);

            var depth = Mathf.Abs(z - cam.transform.position.z);
            var world = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, depth));
            world.z = z;
            return world;
        }

        void OnTurnUpdate(TurnUpdate m)
        {
            if (m.TableId != string.Empty)
            {
                _tipButton.interactable = true;
            }
        }
    }
}
