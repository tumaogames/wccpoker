////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WCC.Core;

namespace WCC.Poker.Client
{
    /// <summary>
    /// This class ay para lang ma test
    /// </summary>
    public class Debug_GameLauncher : MonoBehaviour
    {
        [SerializeField] TMP_InputField _url_inputboxIF;
        [SerializeField] TMP_InputField _operator_inputboxIF;
        [SerializeField] TMP_InputField _matchsizeid_inputboxIF;
        [SerializeField] TMP_InputField _tableCode_inputboxIF;
        [SerializeField] TMP_InputField _token_inputboxIF;
        [SerializeField] Button _enterButton;

        [SerializeField] string _sceneToLoad = "PokerGame";

        /// <summary>
        /// Add listener of the button
        /// Add listener of the token input
        /// Setup all game info for the shared vaut
        /// </summary>
        private void Start()
        {
            _enterButton.onClick.AddListener( () =>
            {
                if (!TryBuildVaultValues())
                    return;

                PokerSharedVault.ServerURL = _url_inputboxIF.text;
                PokerSharedVault.OperatorPublicID = _operator_inputboxIF.text;
                PokerSharedVault.MatchSizeId = int.Parse(_matchsizeid_inputboxIF.text);
                PokerSharedVault.LaunchToken = _token_inputboxIF.text;
                PokerSharedVault.TableCode = _tableCode_inputboxIF.text;

                _enterButton.interactable = false;

                GameServerClient.Configure(PokerSharedVault.ServerURL);
                GameServerClient.ConnectWithLaunchToken(PokerSharedVault.LaunchToken, PokerSharedVault.OperatorPublicID);

                print($"A: {PokerSharedVault.ServerURL} | G: {PokerSharedVault.OperatorPublicID}");

            });
            _token_inputboxIF.onValueChanged.AddListener(OnChangeInput);
            _url_inputboxIF.onValueChanged.AddListener(OnChangeInput);
            _operator_inputboxIF.onValueChanged.AddListener(OnChangeInput);
            _matchsizeid_inputboxIF.onValueChanged.AddListener(OnChangeInput);
            _tableCode_inputboxIF.onValueChanged.AddListener(OnChangeInput);
        }

        /// <summary>
        /// Para mag enable/disable ng button interaction
        /// </summary>
        /// <param name="inputValue"></param>
        void OnChangeInput(string inputValue)
        {
            _enterButton.interactable = IsInputValid();
        }

        /// <summary>
        /// Para direct mag load ng scene
        /// </summary>
        void LoadNewScene()
        {
            SceneManager.LoadScene(_sceneToLoad);
        }

        void OnEnable()
        {
            GameServerClient.ConnectResponseReceivedStatic += OnConnect;
            
           
        }

        void OnDisable()
        {
            GameServerClient.ConnectResponseReceivedStatic -= OnConnect;
        }


        async void OnConnect(ConnectResponse resp)
        {
            PokerSharedVault.PlayerID = resp.PlayerId;
            await Task.Delay(1000);
            LoadNewScene();
        }

        bool IsInputValid()
        {
            if (string.IsNullOrWhiteSpace(_token_inputboxIF.text) || _token_inputboxIF.text.Length < 8)
                return false;
            if (string.IsNullOrWhiteSpace(_url_inputboxIF.text))
                return false;
            if (string.IsNullOrWhiteSpace(_operator_inputboxIF.text))
                return false;
            if (string.IsNullOrWhiteSpace(_matchsizeid_inputboxIF.text))
                return false;
            if (string.IsNullOrWhiteSpace(_tableCode_inputboxIF.text))
                return false;
            return int.TryParse(_matchsizeid_inputboxIF.text, out _);
        }

        bool TryBuildVaultValues()
        {
            if (!IsInputValid())
                return false;
            return true;
        }

    }
}
