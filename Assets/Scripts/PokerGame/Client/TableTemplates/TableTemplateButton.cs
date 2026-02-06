////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class TableTemplateButton : MonoBehaviour
    {
        [SerializeField] TMP_Text _tableNameText;
        [SerializeField] Button _tableButton;
        [SerializeField] Image _tableImage;
        UnityAction<int, Sprite> _onClickFallback;
        [SerializeField] GameObject _checkIcon;

        Sprite _tableSprite;
        int _tableIndex;

        public void InitializeTableTemplateButton(int index, TableTemplateData.TableDesigns template, UnityAction<int, Sprite> _Callback)
        {
            _tableIndex = index;
            _tableNameText.text = template.TableName;
            _tableImage.sprite = template.TableSprite;
            _tableSprite = template.TableSprite;
            _onClickFallback = _Callback;
            _tableButton.onClick.AddListener(OnClick);
        }

        void OnClick() => _onClickFallback?.Invoke(_tableIndex, _tableSprite);

        public void SetCheckEnable(bool isEnableCheck) => _checkIcon.SetActive(isEnableCheck);
    }
}
