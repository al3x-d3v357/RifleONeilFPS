using UnityEngine;

namespace RifleONeilFPS
{
    public enum CubeMovementPattern
    {
        Strafe,
        Orbit,
        ZigZag
    }

    /// <summary>
    /// Challenging multi-HP target cube with dynamic strafe/orbit/zigzag movement,
    /// hit flash feedback, and physics debris shatter on destruction.
    /// </summary>
    public class TargetCube : MonoBehaviour
    {
        [Header("Health & Challenge")]
        public float maxHealth = 3.0f;
        public int shardCount = 10;
        public float explosionForce = 7.5f;
        public AudioClip hitClip;
        public AudioClip destroyClip;

        [Header("Movement")]
        public CubeMovementPattern pattern = CubeMovementPattern.Strafe;
        public float moveSpeed = 2.4f;
        public float moveRadius = 2.6f;
        public float bobAmplitude = 0.45f;
        public float spinSpeed = 55.0f;

        private float _health;
        private Vector3 _anchorPos;
        private float _phase;
        private float _hitFlashTimer;
        private MeshRenderer _mr;
        private Material _baseMat;
        private MaterialPropertyBlock _propBlock;
        private Vector3 _baseScale;

        private void Start()
        {
            _health = maxHealth;
            _anchorPos = transform.position;
            _baseScale = transform.localScale;
            _phase = Random.Range(0.0f, Mathf.PI * 2.0f);
            _mr = GetComponent<MeshRenderer>();
            if (_mr != null) _baseMat = _mr.sharedMaterial;
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            float t = Time.time * moveSpeed + _phase;
            Vector3 offset = Vector3.zero;

            switch (pattern)
            {
                case CubeMovementPattern.Strafe:
                    offset = new Vector3(Mathf.Sin(t) * moveRadius, Mathf.Sin(t * 1.7f) * bobAmplitude, 0.0f);
                    break;
                case CubeMovementPattern.Orbit:
                    offset = new Vector3(Mathf.Cos(t) * moveRadius * 0.7f, Mathf.Sin(t * 1.4f) * bobAmplitude, Mathf.Sin(t) * moveRadius * 0.7f);
                    break;
                case CubeMovementPattern.ZigZag:
                    offset = new Vector3(Mathf.Sin(t * 1.3f) * moveRadius, Mathf.Cos(t * 2.2f) * bobAmplitude, Mathf.Cos(t * 0.7f) * 1.2f);
                    break;
            }

            transform.position = _anchorPos + offset;
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            // Hit flash & subtle scale punch recovery
            if (_hitFlashTimer > 0.0f)
            {
                _hitFlashTimer -= Time.deltaTime;
                if (_mr != null)
                {
                    _propBlock.SetColor("_EmissionColor", new Color(0.63f, 0.88f, 0.94f) * 4.5f);
                    _mr.SetPropertyBlock(_propBlock);
                }
            }
            else if (_mr != null)
            {
                _mr.SetPropertyBlock(null);
            }

            float healthRatio = Mathf.Clamp(_health / maxHealth, 0.45f, 1.0f);
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * (0.75f + 0.25f * healthRatio), Time.deltaTime * 12.0f);
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 shotDirection)
        {
            _health -= amount;
            _hitFlashTimer = 0.08f;
            transform.localScale = _baseScale * 1.12f;

            if (_health <= 0.0f)
            {
                if (destroyClip != null) AudioSource.PlayClipAtPoint(destroyClip, transform.position, 0.85f);
                DestroyCube(hitPoint, shotDirection);
            }
            else if (amount >= 0.5f && hitClip != null)
            {
                AudioSource.PlayClipAtPoint(hitClip, hitPoint, 0.5f);
            }
        }

        private void DestroyCube(Vector3 hitPoint, Vector3 shotDirection)
        {
            SpawnDebris(shotDirection);
            FPSGameManager.Instance?.OnCubeDestroyed();
            Destroy(gameObject);
        }

        private void SpawnDebris(Vector3 shotDirection)
        {
            for (int i = 0; i < shardCount; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "CubeDebris";
                shard.transform.position = transform.position + Random.insideUnitSphere * 0.35f;
                shard.transform.localScale = Vector3.one * Random.Range(0.15f, 0.30f);
                shard.transform.rotation = Random.rotation;

                var mr = shard.GetComponent<MeshRenderer>();
                if (mr != null && _baseMat != null)
                {
                    mr.sharedMaterial = _baseMat;
                }

                var rb = shard.AddComponent<Rigidbody>();
                rb.mass = 0.25f;
                Vector3 impulse = (Random.insideUnitSphere + shotDirection * 0.9f).normalized * explosionForce;
                rb.AddForce(impulse, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 9.0f, ForceMode.Impulse);

                Destroy(shard, 2.0f);
            }
        }
    }
}
