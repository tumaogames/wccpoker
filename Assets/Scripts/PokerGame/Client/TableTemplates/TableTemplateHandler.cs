 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class TableTemplateHandler : MonoBehaviour
    {
        [SerializeField] TableTemplateButton _tableTemplatePrefab;
        [SerializeField] TableTemplateData _tableTemplateData;
        [SerializeField] Transform _tableTemplateContainer;
        [SerializeField] Image _inGameTableImage;
        [SerializeField] GameObject _vipTableTemplatesBtn;
        [SerializeField] bool _isVIP_Room;
        //

        private void Start()
        {
            if(!_isVIP_Room) return;
            _vipTableTemplatesBtn.SetActive(true);
            for (int i = 0; i < _tableTemplateData.Templates.Length; i++)
            {
                var tableBtn = Instantiate(_tableTemplatePrefab, _tableTemplateContainer);
                var d = _tableTemplateData.Templates[i];
                tableBtn.InitializeTableTemplateButton(_tableTemplateData.Templates[i], d =>
                {
                    _inGameTableImage.sprite = d;
                });
            }
        }
    }
}
