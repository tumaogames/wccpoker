using System.Collections;
using UnityEngine;

namespace WCC.Poker.Client
{
    public class EyeBlink : MonoBehaviour
    {
        [SerializeField] Material targetMaterial;

        [Header("Textures")]
        [SerializeField] Texture openEyes;
        [SerializeField] Texture closedEyes;

        [Header("Blink Settings")]
        [SerializeField] float minBlinkDelay = 3f;
        [SerializeField] float maxBlinkDelay = 6f;
        [SerializeField] float blinkDuration = 0.08f;

        void Start()
        {
            StartCoroutine(BlinkLoop());
        }

        IEnumerator BlinkLoop()
        {
            targetMaterial.SetTexture("_BaseMap", openEyes);
            while (true)
            {
                // Wait random time before blinking
                float wait = Random.Range(minBlinkDelay, maxBlinkDelay);
                yield return new WaitForSeconds(wait);

                // Close eyes
                targetMaterial.SetTexture("_BaseMap", closedEyes);

                // Blink time
                yield return new WaitForSeconds(blinkDuration);

                // Open eyes
                targetMaterial.SetTexture("_BaseMap", openEyes);
            }
        }
    }
}