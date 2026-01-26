using DG.Tweening;
using UnityEngine;

public class ButtonPulse : MonoBehaviour
{
    [SerializeField] private float scaleAmount;
    [SerializeField] private float pulseDuration;

    void OnEnable()
    {
        transform.DOScale(scaleAmount, pulseDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
}
