using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RifleONeilFPS
{
    /// <summary>
    /// Dual-mode futuristic rifle:
    /// - Tap/Click Left Mouse: Semi-auto energy pulse shots.
    /// - Hold Left Mouse: Continuous sustained energy beam ("raio feixe").
    /// - Hold Right Mouse: Aim Down Sights (ADS) with FOV zoom & optic alignment.
    /// - R (or empty mag): Reload with timed progress and viewmodel animation.
    /// </summary>
    public class RifleShooter : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public Transform rifleViewmodel;
        public Transform muzzlePoint;
        public Light muzzleFlashLight;
        public LineRenderer tracerLine;
        public RifleReloadAnimator reloadAnimator;

        [Header("Sci-Fi Audio")]
        public AudioSource sfxSource;
        public AudioSource beamLoopSource;
        public AudioClip shotClip;
        public AudioClip reloadOpenClip;
        public AudioClip reloadLaserClip;
        public AudioClip reloadCloseClip;

        [Header("Hip & ADS Framing")]
        public Vector3 hipPosition = new Vector3(0.24f, -0.20f, 0.44f);
        public Vector3 adsPosition = new Vector3(0.0f, -0.155f, 0.32f);
        public float hipFov = 62.0f;
        public float adsFov = 42.0f;
        public float adsLerpSpeed = 12.0f;

        [Header("Single Shot & Beam Settings")]
        public float tapFireCooldown = 0.11f;
        public float holdThreshold = 0.20f;
        public float maxRange = 120.0f;
        public float shotDamage = 1.0f;
        public float beamDps = 6.5f;
        public float beamAmmoPerSecond = 8.0f;

        [Header("Ammo & Reload")]
        public int maxAmmo = 24;
        public float reloadDuration = 2.0f;

        [Header("Recoil & Bob")]
        public float recoilKickZ = 0.045f;
        public float recoilKickRotX = 3.5f;
        public float recoilReturnSpeed = 14.0f;

        public int CurrentAmmo => Mathf.CeilToInt(_ammoFloat);
        public bool IsReloading { get; private set; }
        public float ReloadProgress { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsBeamActive { get; private set; }

        private FPSPlayerController _playerController;
        private float _ammoFloat;
        private float _nextTapTime;
        private float _holdTimer;
        private float _flashTimer;
        private float _bobTimer;
        private Vector3 _currentRecoilOffset;
        private float _currentRecoilRot;
        private Quaternion _baseRifleRot;

        private void Start()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            _playerController = GetComponent<FPSPlayerController>();
            _ammoFloat = maxAmmo;

            if (rifleViewmodel != null)
            {
                rifleViewmodel.localPosition = hipPosition;
                _baseRifleRot = rifleViewmodel.localRotation;
            }
            if (tracerLine != null) tracerLine.enabled = false;
            if (muzzleFlashLight != null) muzzleFlashLight.intensity = 0.0f;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null || kb == null) return;

            // Reload input or auto-reload when empty
            if (!IsReloading && ((kb.rKey.wasPressedThisFrame && CurrentAmmo < maxAmmo) || _ammoFloat <= 0.01f))
            {
                StartCoroutine(ReloadRoutine());
            }

            // ADS (Right Mouse Button)
            IsAiming = !IsReloading && mouse.rightButton.isPressed;
            if (playerCamera != null)
            {
                float targetFov = IsAiming ? adsFov : hipFov;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * adsLerpSpeed);
            }

            // Firing logic (Tap = single pulse shot, Hold = continuous beam)
            IsBeamActive = false;
            if (!IsReloading && _ammoFloat > 0.01f)
            {
                if (mouse.leftButton.wasPressedThisFrame && Time.time >= _nextTapTime)
                {
                    _nextTapTime = Time.time + tapFireCooldown;
                    _holdTimer = 0.0f;
                    FireSingleShot();
                }
                else if (mouse.leftButton.isPressed)
                {
                    _holdTimer += Time.deltaTime;
                    if (_holdTimer >= holdThreshold)
                    {
                        IsBeamActive = true;
                        FireContinuousBeam(Time.deltaTime);
                    }
                }
                else
                {
                    _holdTimer = 0.0f;
                }
            }
            else
            {
                _holdTimer = 0.0f;
            }

            UpdateVisuals(Time.deltaTime);
        }

        private void FireSingleShot()
        {
            _ammoFloat = Mathf.Max(0.0f, _ammoFloat - 1.0f);
            _currentRecoilOffset = new Vector3(0.0f, 0.006f, -recoilKickZ);
            _currentRecoilRot = recoilKickRotX;
            _flashTimer = 0.06f;
            if (sfxSource != null && shotClip != null)
            {
                sfxSource.pitch = Random.Range(0.95f, 1.08f);
                sfxSource.PlayOneShot(shotClip, 0.7f);
            }

            if (playerCamera == null) return;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
            Vector3 endPoint = ray.origin + ray.direction * maxRange;

            if (Physics.Raycast(ray, out RaycastHit hit, maxRange))
            {
                endPoint = hit.point;
                var target = hit.collider.GetComponent<TargetCube>();
                if (target != null)
                {
                    target.TakeDamage(shotDamage, hit.point, ray.direction);
                    FPSGameManager.Instance?.NotifyHit();
                }
            }

            StartCoroutine(ShowTapTracer(endPoint));
        }

        private void FireContinuousBeam(float dt)
        {
            _ammoFloat = Mathf.Max(0.0f, _ammoFloat - beamAmmoPerSecond * dt);
            _flashTimer = 0.04f;

            // Subtle continuous beam vibration
            _currentRecoilOffset = new Vector3(
                Random.Range(-0.002f, 0.002f),
                Random.Range(-0.002f, 0.002f),
                -0.018f
            );
            _currentRecoilRot = 0.9f;

            if (playerCamera == null) return;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
            Vector3 endPoint = ray.origin + ray.direction * maxRange;

            if (Physics.Raycast(ray, out RaycastHit hit, maxRange))
            {
                endPoint = hit.point;
                var target = hit.collider.GetComponent<TargetCube>();
                if (target != null)
                {
                    target.TakeDamage(beamDps * dt, hit.point, ray.direction);
                    FPSGameManager.Instance?.NotifyHit();
                }
            }

            if (tracerLine != null && muzzlePoint != null)
            {
                float pulse = 0.026f + Mathf.Sin(Time.time * 45.0f) * 0.008f;
                tracerLine.enabled = true;
                tracerLine.startWidth = pulse;
                tracerLine.endWidth = pulse * 0.7f;
                tracerLine.SetPosition(0, muzzlePoint.position);
                tracerLine.SetPosition(1, endPoint);
            }
        }

        private IEnumerator ShowTapTracer(Vector3 endPoint)
        {
            if (tracerLine != null && muzzlePoint != null)
            {
                tracerLine.enabled = true;
                tracerLine.startWidth = 0.022f;
                tracerLine.endWidth = 0.010f;
                tracerLine.SetPosition(0, muzzlePoint.position);
                tracerLine.SetPosition(1, endPoint);
            }

            yield return new WaitForSeconds(0.055f);

            if (!IsBeamActive && tracerLine != null)
            {
                tracerLine.enabled = false;
            }
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            ReloadProgress = 0.0f;
            if (tracerLine != null) tracerLine.enabled = false;
            if (beamLoopSource != null && beamLoopSource.isPlaying) beamLoopSource.Stop();

            if (sfxSource != null && reloadOpenClip != null)
            {
                sfxSource.pitch = 1.0f;
                sfxSource.PlayOneShot(reloadOpenClip, 0.8f);
            }

            bool playedLaser = false;
            bool playedClose = false;
            float elapsed = 0.0f;
            while (elapsed < reloadDuration)
            {
                elapsed += Time.deltaTime;
                ReloadProgress = Mathf.Clamp01(elapsed / reloadDuration);

                if (!playedLaser && ReloadProgress >= 0.22f)
                {
                    playedLaser = true;
                    if (sfxSource != null && reloadLaserClip != null)
                        sfxSource.PlayOneShot(reloadLaserClip, 0.85f);
                }
                if (!playedClose && ReloadProgress >= 0.76f)
                {
                    playedClose = true;
                    if (sfxSource != null && reloadCloseClip != null)
                        sfxSource.PlayOneShot(reloadCloseClip, 0.85f);
                }

                yield return null;
            }

            _ammoFloat = maxAmmo;
            ReloadProgress = 1.0f;
            IsReloading = false;
            if (reloadAnimator != null) reloadAnimator.SetReloadProgress(false, 0.0f);
        }

        private void UpdateVisuals(float dt)
        {
            if (reloadAnimator != null)
            {
                reloadAnimator.SetReloadProgress(IsReloading, ReloadProgress);
            }

            if (beamLoopSource != null)
            {
                if (IsBeamActive && !beamLoopSource.isPlaying) beamLoopSource.Play();
                else if (!IsBeamActive && beamLoopSource.isPlaying) beamLoopSource.Stop();
            }

            if (!IsBeamActive && tracerLine != null && _flashTimer <= 0.0f)
            {
                tracerLine.enabled = false;
            }

            if (muzzleFlashLight != null)
            {
                if (_flashTimer > 0.0f)
                {
                    _flashTimer -= dt;
                    muzzleFlashLight.intensity = IsBeamActive ? 4.5f : 7.0f;
                }
                else
                {
                    muzzleFlashLight.intensity = 0.0f;
                }
            }

            _currentRecoilOffset = Vector3.Lerp(_currentRecoilOffset, Vector3.zero, dt * recoilReturnSpeed);
            _currentRecoilRot = Mathf.Lerp(_currentRecoilRot, 0.0f, dt * recoilReturnSpeed);

            // Weapon walk bob
            Vector3 bobOffset = Vector3.zero;
            if (_playerController != null && _playerController.IsMoving && !IsReloading)
            {
                float bobScale = IsAiming ? 0.25f : 1.0f;
                _bobTimer += dt * 9.0f * _playerController.CurrentSpeedRatio;
                bobOffset = new Vector3(
                    Mathf.Cos(_bobTimer * 0.5f) * 0.008f * bobScale,
                    Mathf.Sin(_bobTimer) * 0.007f * bobScale,
                    0.0f
                );
            }

            if (rifleViewmodel != null)
            {
                Vector3 targetBasePos = IsAiming ? adsPosition : hipPosition;
                if (IsReloading)
                {
                    targetBasePos += new Vector3(0.02f, -0.09f, -0.04f);
                }

                Vector3 desiredPos = targetBasePos + _currentRecoilOffset + bobOffset;
                rifleViewmodel.localPosition = Vector3.Lerp(rifleViewmodel.localPosition, desiredPos, dt * adsLerpSpeed);

                float reloadTilt = IsReloading ? Mathf.Sin(ReloadProgress * Mathf.PI) * 28.0f : 0.0f;
                Quaternion desiredRot = _baseRifleRot * Quaternion.Euler(-_currentRecoilRot + reloadTilt, 0.0f, reloadTilt * 0.6f);
                rifleViewmodel.localRotation = Quaternion.Slerp(rifleViewmodel.localRotation, desiredRot, dt * adsLerpSpeed);
            }
        }
    }
}
