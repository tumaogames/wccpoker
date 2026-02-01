using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectFitter : MonoBehaviour
{
    public SpriteRenderer targetSprite; // Assign your sprite here
    private Camera cam;
    private Vector2 lastScreenSize;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
        lastScreenSize = new Vector2(Screen.width, Screen.height);
    }

    void Update()
    {
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            UpdateCameraSize();
            lastScreenSize = new Vector2(Screen.width, Screen.height);
        }
    }

    void UpdateCameraSize()
    {
        if (targetSprite == null)
        {
            Debug.LogWarning("CameraAspectFitter: No targetSprite assigned.");
            return;
        }

        // Sprite size in pixels
        float spritePixelWidth = targetSprite.sprite.rect.width;
        float spritePixelHeight = targetSprite.sprite.rect.height;

        // Pixels per unit
        //float ppu = targetSprite.sprite.pixelsPerUnit;

        // Sprite size in world units
        float spriteWorldWidth = spritePixelWidth / 192.17f;
        float spriteWorldHeight = spritePixelHeight / 192.17f;

        // Screen aspect ratio (width/height)
        float screenAspect = (float)Screen.width / Screen.height;

        // Sprite aspect ratio (width/height)
        float spriteAspect = spriteWorldWidth / spriteWorldHeight;

        if (screenAspect >= spriteAspect)
        {
            // Screen is wider: fit by height
            cam.orthographicSize = spriteWorldHeight / 2f;
        }
        else
        {
            // Screen is narrower: fit by width
            cam.orthographicSize = (spriteWorldWidth / screenAspect) / 2f;
        }
    }
}



