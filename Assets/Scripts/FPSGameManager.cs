using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RifleONeilFPS
{
    /// <summary>
    /// Manages escalating waves of challenging target cubes, hitmarkers, ammo HUD, and reload bar.
    /// </summary>
    public class FPSGameManager : MonoBehaviour
    {
        public static FPSGameManager Instance { get; private set; }

        [Header("Wave Settings")]
        public int baseTargetCount = 12;
        public Material targetCubeMaterial;
        public AudioClip cubeHitClip;
        public AudioClip cubeDestroyClip;
        public Vector2 spawnAreaX = new Vector2(-14.0f, 14.0f);
        public Vector2 spawnAreaZ = new Vector2(2.0f, 16.0f);
        public Vector2 spawnHeightY = new Vector2(1.4f, 4.4f);

        private readonly List<GameObject> _activeCubes = new List<GameObject>();
        private RifleShooter _shooter;
        private int _wave = 1;
        private int _waveTotal;
        private int _waveDestroyed;
        private int _totalScore;
        private float _hitMarkerTimer;
        private float _waveBannerTimer = 2.0f;

        private GUIStyle _hudStyle;
        private GUIStyle _subStyle;
        private GUIStyle _hintStyle;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _shooter = FindFirstObjectByType<RifleShooter>();
            var existing = FindObjectsByType<TargetCube>(FindObjectsSortMode.None);
            if (existing.Length > 0)
            {
                foreach (var c in existing) _activeCubes.Add(c.gameObject);
                _waveTotal = existing.Length;
            }
            else
            {
                SpawnWave(_wave);
            }
        }

        private void Update()
        {
            if (_hitMarkerTimer > 0.0f) _hitMarkerTimer -= Time.deltaTime;
            if (_waveBannerTimer > 0.0f) _waveBannerTimer -= Time.deltaTime;

            var kb = Keyboard.current;
            if (kb != null && kb.tKey.wasPressedThisFrame)
            {
                _wave = 1;
                _totalScore = 0;
                SpawnWave(_wave);
            }
        }

        public void NotifyHit()
        {
            _hitMarkerTimer = 0.10f;
        }

        public void OnCubeDestroyed()
        {
            _waveDestroyed++;
            _totalScore += 100 * _wave;

            if (_waveDestroyed >= _waveTotal)
            {
                _wave++;
                SpawnWave(_wave);
            }
        }

        private void SpawnWave(int waveNum)
        {
            foreach (var c in _activeCubes)
            {
                if (c != null) Destroy(c);
            }
            _activeCubes.Clear();
            _waveDestroyed = 0;
            _waveTotal = baseTargetCount + (waveNum - 1) * 4;
            _waveBannerTimer = 2.2f;

            for (int i = 0; i < _waveTotal; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Target_Cube_W{waveNum}_{i + 1:00}";
                cube.transform.position = new Vector3(
                    Random.Range(spawnAreaX.x, spawnAreaX.y),
                    Random.Range(spawnHeightY.x, spawnHeightY.y),
                    Random.Range(spawnAreaZ.x, spawnAreaZ.y)
                );

                float scale = Mathf.Max(0.62f, 0.95f - (waveNum - 1) * 0.08f);
                cube.transform.localScale = Vector3.one * scale;

                var mr = cube.GetComponent<MeshRenderer>();
                if (mr != null && targetCubeMaterial != null)
                {
                    mr.sharedMaterial = targetCubeMaterial;
                }

                var tc = cube.AddComponent<TargetCube>();
                tc.maxHealth = 2.5f + (waveNum - 1) * 1.0f;
                tc.moveSpeed = 2.0f + (waveNum - 1) * 0.65f;
                tc.moveRadius = 2.2f + (waveNum - 1) * 0.45f;
                tc.pattern = (CubeMovementPattern)(i % 3);
                tc.hitClip = cubeHitClip;
                tc.destroyClip = cubeDestroyClip;

                _activeCubes.Add(cube);
            }
        }

        private void OnGUI()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _hudStyle.normal.textColor = new Color(0.63f, 0.88f, 0.94f, 1.0f);

                _subStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
                _subStyle.normal.textColor = Color.white;

                _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                _hintStyle.normal.textColor = new Color(0.85f, 0.92f, 0.98f, 0.9f);
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            // Dynamic Crosshair (tightens on ADS)
            bool aiming = _shooter != null && _shooter.IsAiming;
            float gap = aiming ? 2.5f : 5.5f;
            float len = aiming ? 5.0f : 7.0f;
            GUI.color = new Color(0.38f, 0.76f, 0.90f, 0.95f);
            GUI.DrawTexture(new Rect(cx - gap - len, cy - 1, len, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + gap, cy - 1, len, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy - gap - len, 2, len), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy + gap, 2, len), Texture2D.whiteTexture);

            // Hitmarker flash
            if (_hitMarkerTimer > 0.0f)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(cx - 8, cy - 8, 4, 4), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + 4, cy - 8, 4, 4), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - 8, cy + 4, 4, 4), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + 4, cy + 4, 4, 4), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            // Top-left Score & Wave
            GUI.Label(new Rect(24, 18, 440, 30), $"ONDA {_wave}  |  CUBOS: {_waveDestroyed} / {_waveTotal}  |  PONTOS: {_totalScore}", _hudStyle);
            GUI.Label(new Rect(24, 50, 760, 24), "WASD: Mover | Clique Esq: Tiro | Segurar Esq: Raio Feixe | Dir: Mira ADS | R: Recarregar | T: Reiniciar", _hintStyle);

            // Bottom-right Ammo & Reload Bar
            if (_shooter != null)
            {
                float bx = Screen.width - 260;
                float by = Screen.height - 76;
                string modeLabel = _shooter.IsBeamActive ? "MODO: RAIO FEIXE" : (aiming ? "MIRA ADS ATIVA" : "MODO: PULSO");
                GUI.Label(new Rect(bx, by - 26, 240, 24), modeLabel, _hintStyle);
                GUI.Label(new Rect(bx, by, 240, 28), $"MUNIÇÃO: {_shooter.CurrentAmmo} / {_shooter.maxAmmo}", _hudStyle);

                if (_shooter.IsReloading)
                {
                    GUI.Label(new Rect(cx - 85, cy + 36, 180, 24), "RECARREGANDO...", _subStyle);
                    GUI.color = new Color(0.11f, 0.21f, 0.36f, 0.85f);
                    GUI.DrawTexture(new Rect(cx - 80, cy + 64, 160, 10), Texture2D.whiteTexture);
                    GUI.color = new Color(0.63f, 0.88f, 0.94f, 1.0f);
                    GUI.DrawTexture(new Rect(cx - 80, cy + 64, 160 * _shooter.ReloadProgress, 10), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }

            if (_waveBannerTimer > 0.0f)
            {
                GUI.Label(new Rect(cx - 90, 90, 220, 34), $"ONDA {_wave} INICIADA!", _hudStyle);
            }
        }
    }
}
