////////////////////
//       RECK       //
////////////////////


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class TableTemplateHandler : MonoBehaviour
    {
        [SerializeField] TableTemplateButton _tableTemplatePrefab;
        [SerializeField] TableTemplateData _tableTemplateData;
        [SerializeField] Transform[] _tableTemplateContainers;
        [SerializeField] Image _inGameTableImage;
        [SerializeField] GameObject _vipTableTemplatesBtn;
        [SerializeField] bool _isVIP_Room;
        //

        readonly List<TableTemplateButton> _tableInstanceList = new();
        readonly Dictionary<int, List<TableTemplateButton>> _tableInstancesByIndex = new();

        private void Start()
        {
            if(!_isVIP_Room) return;
            _vipTableTemplatesBtn.SetActive(true);
            for (int i = 0; i < _tableTemplateData.Templates.Length; i++)
            {
                for (int j = 0; j < _tableTemplateContainers.Length; j++)
                {
                    var tableBtn = Instantiate(_tableTemplatePrefab, _tableTemplateContainers[j]);
                    _tableInstanceList.Add(tableBtn);
                    var d = _tableTemplateData.Templates[i];
                    if (!_tableInstancesByIndex.TryGetValue(i, out var list))
                    {
                        list = new List<TableTemplateButton>();
                        _tableInstancesByIndex[i] = list;
                    }
                    list.Add(tableBtn);
                    tableBtn.InitializeTableTemplateButton(i, _tableTemplateData.Templates[i], (index, sprite) =>
                    {
                        _inGameTableImage.sprite = sprite;
                        SetChooseTable(index);
                    });
                }
            }

            if (_tableInstanceList.Count == 0) return;

            _tableInstanceList[0].SetCheckEnable(true);
        }

        void SetChooseTable(int index)
        {
            _tableInstanceList.ForEach(i => i.SetCheckEnable(false));
            if (_tableInstancesByIndex.TryGetValue(index, out var list))
            {
                foreach (var btn in list)
                    btn.SetCheckEnable(true);
            }
        }
    }
}
