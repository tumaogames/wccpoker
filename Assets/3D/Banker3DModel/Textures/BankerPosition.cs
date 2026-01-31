 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


public class BankerPosition : MonoBehaviour
{
    [SerializeField] Transform _banker;
    [SerializeField] Vector3 _positionOffset;
    Vector3 _baseLocalPosition;

    private void Awake()
    {
        if (_banker == null)
        {
            _banker = transform;
        }

        _baseLocalPosition = _banker.localPosition;
    }

    private void LateUpdate()
    {
        if (_banker == null) return;
        _banker.localPosition = _baseLocalPosition + _positionOffset;
    }
}
