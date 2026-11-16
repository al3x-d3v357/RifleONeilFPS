using System.Collections.Generic;
using UnityEngine;

namespace RifleONeilFPS
{
    /// <summary>
    /// Animates the front disassembly, glowing laser recharge core, and reassembly
    /// of the O'NEIL rifle during reload.
    /// </summary>
    public class RifleReloadAnimator : MonoBehaviour
    {
        private class PartState
        {
            public Transform transform;
            public Vector3 baseLocalPos;
            public Quaternion baseLocalRot;
            public Vector3 targetWorldDirOffset; // In Rifle_Viewmodel local space
            public Vector3 targetEulerOffset;
        }

        [Header("References")]
        public Transform rifleViewmodel;
        public Transform rifleModelRoot;
        public Light reloadLight;
        public Transform rechargeLaserBeam;

        [Header("Animation Settings")]
        public float explodeDistance = 0.075f;

        private readonly List<PartState> _parts = new List<PartState>();
        private Vector3 _beamBaseScale;

        private void Start()
        {
            if (rifleViewmodel == null) rifleViewmodel = transform;
            if (rifleModelRoot == null)
            {
                var found = rifleViewmodel.Find("Rifle_ONeil_Model");
                if (found != null) rifleModelRoot = found;
            }

            if (rifleModelRoot != null)
            {
                CacheFrontParts(rifleModelRoot);
            }

            if (reloadLight != null) reloadLight.intensity = 0.0f;
            if (rechargeLaserBeam != null)
            {
                _beamBaseScale = rechargeLaserBeam.localScale;
                rechargeLaserBeam.gameObject.SetActive(false);
            }
        }

        private void CacheFrontParts(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root) continue;
                string n = t.name;

                Vector3 vmOffset = Vector3.zero;
                Vector3 eulerOffset = Vector3.zero;

                if (n.Contains("Upper_Shroud_Front") || n.Contains("Upper_Shroud_Flank") || n.Contains("Upper_Shroud_Rib"))
                {
                    vmOffset = new Vector3(0.0f, 0.075f, 0.055f);
                    eulerOffset = new Vector3(-14.0f, 0.0f, 0.0f);
                }
                else if (n.Contains("Lower_Shroud_Blue") || n.Contains("Lower_Skid_Grey"))
                {
                    vmOffset = new Vector3(0.0f, -0.075f, 0.045f);
                    eulerOffset = new Vector3(14.0f, 0.0f, 0.0f);
                }
                else if ((n.Contains("Truss_") || n.Contains("Forend_Mech") || n.Contains("Forend_Latch") || n.Contains("Muzzle_Neon_Strip")) && n.Contains("_L"))
                {
                    vmOffset = new Vector3(-0.070f, 0.0f, 0.035f);
                    eulerOffset = new Vector3(0.0f, -12.0f, 0.0f);
                }
                else if ((n.Contains("Truss_") || n.Contains("Forend_Mech") || n.Contains("Forend_Latch") || n.Contains("Muzzle_Neon_Strip")) && n.Contains("_R"))
                {
                    vmOffset = new Vector3(0.070f, 0.0f, 0.035f);
                    eulerOffset = new Vector3(0.0f, 12.0f, 0.0f);
                }
                else if (n.Contains("Muzzle_Bore") || n.Contains("Muzzle_Neon_Ring") || n.Contains("Truss_Inner_Piston"))
                {
                    vmOffset = new Vector3(0.0f, 0.0f, 0.085f);
                }

                if (vmOffset.sqrMagnitude > 0.00001f)
                {
                    _parts.Add(new PartState
                    {
                        transform = t,
                        baseLocalPos = t.localPosition,
                        baseLocalRot = t.localRotation,
                        targetWorldDirOffset = vmOffset,
                        targetEulerOffset = eulerOffset
                    });
                }
            }
        }

        /// <summary>
        /// Updates the disassembly -> laser recharge -> reassembly animation.
        /// progress: 0..1 while reloading, or 0 when idle.
        /// </summary>
        public void SetReloadProgress(bool isReloading, float progress)
        {
            if (!isReloading)
            {
                ApplyPose(0.0f, 0.0f);
                return;
            }

            // 0.00 -> 0.25: Disassemble open
            // 0.25 -> 0.75: Hold open & emit recharging laser
            // 0.75 -> 1.00: Reassemble & lock
            float openFactor;
            if (progress < 0.25f)
            {
                openFactor = Mathf.SmoothStep(0.0f, 1.0f, progress / 0.25f);
            }
            else if (progress < 0.75f)
            {
                openFactor = 1.0f;
            }
            else
            {
                openFactor = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, (progress - 0.75f) / 0.25f);
            }

            float laserFactor = 0.0f;
            if (progress >= 0.20f && progress <= 0.80f)
            {
                float norm = (progress - 0.20f) / 0.60f;
                laserFactor = Mathf.Sin(norm * Mathf.PI);
            }

            ApplyPose(openFactor, laserFactor);
        }

        private void ApplyPose(float openFactor, float laserFactor)
        {
            if (rifleModelRoot != null && rifleViewmodel != null)
            {
                foreach (var p in _parts)
                {
                    if (p.transform == null) continue;
                    // Convert offset from Rifle_Viewmodel space to p.transform.parent local space
                    Vector3 worldDelta = rifleViewmodel.TransformVector(p.targetWorldDirOffset * openFactor);
                    Vector3 localDelta = p.transform.parent.InverseTransformVector(worldDelta);
                    p.transform.localPosition = p.baseLocalPos + localDelta;
                    p.transform.localRotation = p.baseLocalRot * Quaternion.Euler(p.targetEulerOffset * openFactor);
                }
            }

            if (reloadLight != null)
            {
                float pulse = 1.0f + Mathf.Sin(Time.time * 28.0f) * 0.25f;
                reloadLight.intensity = laserFactor * 12.0f * pulse;
            }

            if (rechargeLaserBeam != null)
            {
                bool showBeam = laserFactor > 0.05f;
                if (rechargeLaserBeam.gameObject.activeSelf != showBeam)
                {
                    rechargeLaserBeam.gameObject.SetActive(showBeam);
                }
                if (showBeam)
                {
                    float w = (0.7f + Mathf.Sin(Time.time * 32.0f) * 0.3f) * laserFactor;
                    rechargeLaserBeam.localScale = new Vector3(_beamBaseScale.x * w, _beamBaseScale.y, _beamBaseScale.z * w);
                }
            }
        }
    }
}
