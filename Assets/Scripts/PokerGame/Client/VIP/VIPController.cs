 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class VIPController : MonoBehaviour
    {
       
        [SerializeField] bool _isVIPRoom;

        [SerializeField] GameObject _vipGroup;

        [SerializeField] Button _createButton;
        //

        private void Start()
        {
            if(!_isVIPRoom) return;
            _vipGroup.SetActive(true);

            _createButton.onClick.AddListener(() =>
            {
                _vipGroup.SetActive(false);
            });
        }
    }
}
