 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


namespace WCC.Poker.Client
{
    public class PlayerHUDController : MonoBehaviour
    {

        [SerializeField] PlayerHUD_UI _playerHUDPrefab;
        [SerializeField] Transform[] _playersTablePositions;
        [SerializeField] Transform _playersContainer;
        [SerializeField] int _maxPlayers = 3;

        [SerializeField] Sprite[] _sampleAvatars;

        #region EDITOR
        private void OnValidate()
        {
            if(_maxPlayers > _playersTablePositions.Length) _maxPlayers = _playersTablePositions.Length;
        }
        #endregion EDITOR

        private void Start()
        {
            for (int i = 0; i < _maxPlayers; i++)
            {
                SummonPlayerHUDUI(i);
            }
        }

        /// <summary>
        /// This function ay para mag instantiate ng player UIs
        /// </summary>
        /// <param name="i"></param>
        void SummonPlayerHUDUI(int i)
        {
            var p = Instantiate(_playerHUDPrefab, _playersContainer);
            p.transform.localPosition = i == 0 ? _playersTablePositions[0].localPosition : _playersTablePositions[i].localPosition;

            p.InititalizePlayerHUDUI("ID3423", "SampleName", i == 0, 1, _sampleAvatars[UnityEngine.Random.Range(1, _sampleAvatars.Length)], UnityEngine.Random.Range(100, 999));
        }
    }
}
