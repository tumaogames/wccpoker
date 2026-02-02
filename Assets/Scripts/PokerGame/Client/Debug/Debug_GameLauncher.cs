////////////////////
//       RECK       //
////////////////////


using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WCC.Core;

namespace WCC.Poker.Client
{
    public class Debug_GameLauncher : MonoBehaviour
    {
        [SerializeField] TMP_InputField _url_inputboxIF;
        [SerializeField] TMP_InputField _operator_inputboxIF;
        [SerializeField] TMP_InputField _matchsizeid_inputboxIF;
        [SerializeField] TMP_InputField _token_inputboxIF;
        [SerializeField] Button _enterButton;

        [SerializeField] string _sceneToLoad = "PokerGame";

        private void Start()
        {
            _enterButton.onClick.AddListener(async () =>
            {
                PokerSharedVault.ServerURL = _url_inputboxIF.text;
                PokerSharedVault.OperatorPublicID = _operator_inputboxIF.text;
                PokerSharedVault.MatchSizeId = int.Parse(_matchsizeid_inputboxIF.text);
                PokerSharedVault.LaunchToken = _token_inputboxIF.text;

                _enterButton.interactable = false;
                await Task.Delay(1000);

                LoadNewScene();
            });
            _token_inputboxIF.onValueChanged.AddListener(OnChangeInput);
        }

        void OnChangeInput(string inputValue)
        {
            _enterButton.interactable = inputValue != string.Empty && inputValue.Length >= 8 && _url_inputboxIF.text.Length > 0 && _operator_inputboxIF.text.Length > 0 && _matchsizeid_inputboxIF.text.Length > 0;
        }

        void LoadNewScene()
        {
            SceneManager.LoadScene(_sceneToLoad);
        }
    }
}
