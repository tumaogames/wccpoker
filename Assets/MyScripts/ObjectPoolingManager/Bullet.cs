using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour, IPooledObject
{
    [Header("Bullet Settings")]
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void OnObjectSpawn()
    {
        ResetBullet();
        Invoke(nameof(Deactivate), lifeTime);
    }

    private void ResetBullet()
    {
        // Optional: reset velocity or other transient states
        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        CancelInvoke();
    }

    private void Deactivate()
    {
        // Optional: add visual/audio effects before deactivation
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CancelInvoke(); // Safety net in case disabled before lifetime ends
    }
}
