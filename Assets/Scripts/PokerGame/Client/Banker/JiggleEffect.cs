////////////////////
//       RECK       //
////////////////////


using NaughtyAttributes;
using UnityEngine;


namespace WCC.Poker.Client
{
    public class JiggleEffect : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer _skinnedMesh;
        [SerializeField] string _jiggleUpName = "Jiggle_Up";
        [SerializeField] string _jiggleDownName = "Jiggle_Down";
        [SerializeField] float _jiggleTarget = 100f;
        [SerializeField] float _jiggleUpDuration = 0.2f;
        [SerializeField] float _jiggleDownDuration = 0.2f;
        //

        Coroutine _fadeRoutine;

        [Button]
        public void AnimateJiggle()
        {
            if (_skinnedMesh == null || _skinnedMesh.sharedMesh == null) return;

            int upIndex = _skinnedMesh.sharedMesh.GetBlendShapeIndex(_jiggleUpName);
            int downIndex = _skinnedMesh.sharedMesh.GetBlendShapeIndex(_jiggleDownName);
            if (upIndex < 0 && downIndex < 0) return;

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(AnimateJiggleRoutine(upIndex, downIndex, _jiggleTarget, _jiggleUpDuration, _jiggleDownDuration));
        }

        System.Collections.IEnumerator AnimateJiggleRoutine(int upIndex, int downIndex, float target, float upDuration, float downDuration)
        {
            if (upIndex >= 0) _skinnedMesh.SetBlendShapeWeight(upIndex, 0f);
            if (downIndex >= 0) _skinnedMesh.SetBlendShapeWeight(downIndex, 0f);

            if (upIndex >= 0)
            {
                yield return AnimateBlendShape(upIndex, target, upDuration, downDuration);
            }

            if (downIndex >= 0)
            {
                yield return AnimateBlendShape(downIndex, target, upDuration, downDuration);
            }
        }

        System.Collections.IEnumerator AnimateBlendShape(int blendShapeIndex, float target, float upDuration, float downDuration)
        {
            float elapsed = 0f;
            while (elapsed < upDuration)
            {
                elapsed += Time.deltaTime;
                float t = upDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / upDuration);
                _skinnedMesh.SetBlendShapeWeight(blendShapeIndex, Mathf.Lerp(0f, target, t));
                yield return null;
            }

            _skinnedMesh.SetBlendShapeWeight(blendShapeIndex, target);

            elapsed = 0f;
            while (elapsed < downDuration)
            {
                elapsed += Time.deltaTime;
                float t = downDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / downDuration);
                _skinnedMesh.SetBlendShapeWeight(blendShapeIndex, Mathf.Lerp(target, 0f, t));
                yield return null;
            }

            _skinnedMesh.SetBlendShapeWeight(blendShapeIndex, 0f);
        }

    }
}
