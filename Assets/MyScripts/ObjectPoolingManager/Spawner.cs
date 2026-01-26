using UnityEngine;

public class Spawner : MonoBehaviour
{
    public string poolTag = "Bullet";
    public float spawnRate = 1f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnRate)
        {
            ObjectPoolManager.Instance.SpawnFromPool(poolTag, transform.position, Quaternion.identity);
            timer = 0f;
        }
    }
}
