using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TokenPopup : MonoBehaviour
{
    public TMP_InputField tokenInput;
    public SceneLoader sceneLoader;
    public Button confirmButton;
    public GameObject popupRoot;

    private void Awake()
    {
        WireReferences();
    }

    private void OnEnable()
    {
        WireReferences();
    }

    private void WireReferences()
    {
        if (sceneLoader == null)
        {
            sceneLoader = FindObjectOfType<SceneLoader>();
        }

        if (popupRoot == null && sceneLoader != null)
        {
            popupRoot = sceneLoader.tokenPopup;
        }

        if (tokenInput == null)
        {
            if (popupRoot != null)
            {
                tokenInput = popupRoot.GetComponentInChildren<TMP_InputField>(true);
            }
            else
            {
                tokenInput = GetComponentInChildren<TMP_InputField>(true);
            }
        }

        if (confirmButton == null)
        {
            Button[] buttons;
            if (popupRoot != null)
            {
                buttons = popupRoot.GetComponentsInChildren<Button>(true);
            }
            else
            {
                buttons = GetComponentsInChildren<Button>(true);
            }

            if (buttons.Length > 0)
            {
                foreach (var btn in buttons)
                {
                    var name = btn.gameObject.name;
                    if (name.IndexOf("ok", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("confirm", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        confirmButton = btn;
                        break;
                    }
                }

                if (confirmButton == null)
                {
                    confirmButton = buttons[0];
                }
            }
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Confirm);
            confirmButton.onClick.AddListener(Confirm);
        }
        else
        {
            Debug.LogError("TokenPopup could not find a confirm Button to wire.");
        }
    }

    public void Confirm()
    {
        if (tokenInput == null)
        {
            Debug.LogError("TokenPopup missing tokenInput.");
            return;
        }

        var token = tokenInput.text.Trim();
        Debug.Log("TokenPopup Confirm clicked. Token length: " + token.Length);

        if (sceneLoader != null)
        {
            sceneLoader.ConfirmToken(token);
        }
        else
        {
            TokenManager.EnsureInstance();
            TokenManager.Instance.SetToken(token);
        }

        // Hide the popup UI (not the TokenManager object).
        if (sceneLoader != null && sceneLoader.tokenPopup != null)
        {
            sceneLoader.tokenPopup.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}


