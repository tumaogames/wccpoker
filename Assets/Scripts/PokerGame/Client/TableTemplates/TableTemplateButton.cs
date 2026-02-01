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
        UnityAction<Sprite> _onClickFallback;

        Sprite _tableSprite;

        public void InitializeTableTemplateButton(TableTemplateData.TableDesigns template, UnityAction<Sprite> _Callback)
        {
            _tableNameText.text = template.TableName;
            _tableImage.sprite = template.TableSprite;
            _tableSprite = template.TableSprite;
            _onClickFallback = _Callback;
            _tableButton.onClick.AddListener(OnClick);
        }

        void OnClick() => _onClickFallback?.Invoke(_tableSprite);
    }
}
