using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RifleONeilFPS.Editor
{
    public static class FPSSceneSetup
    {
        [MenuItem("Tools/Setup FPS Arena")]
        public static void BuildArena()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1. Create URP Lit Materials matching the user's palette (#1B365D, #005A9C, #60C1E5, #A0E0EF)
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Scenes");

            Material matNavy = CreateURPMaterial("Assets/Materials/Mat_Rifle_Navy.mat", "#1B365D", 0.35f, 0.50f);
            Material matBlue = CreateURPMaterial("Assets/Materials/Mat_Rifle_Blue_Metallic.mat", "#005A9C", 0.85f, 0.24f);
            Material matSky = CreateURPMaterial("Assets/Materials/Mat_Rifle_Sky_Metallic.mat", "#60C1E5", 0.55f, 0.32f);
            Material matIce = CreateURPMaterial("Assets/Materials/Mat_Rifle_Ice_Decal.mat", "#A0E0EF", 0.05f, 0.30f);
            Material matNeon = CreateURPMaterial("Assets/Materials/Mat_Rifle_Neon_Cyan.mat", "#A0E0EF", 0.0f, 0.15f, emissionHex: "#A0E0EF", emissionIntensity: 4.0f);
            Material matFloor = CreateURPMaterial("Assets/Materials/Mat_Arena_Floor.mat", "#18202C", 0.10f, 0.72f);
            Material matWall = CreateURPMaterial("Assets/Materials/Mat_Arena_Wall.mat", "#223044", 0.20f, 0.60f);
            Material matTarget = CreateURPMaterial("Assets/Materials/Mat_Target_Cube.mat", "#FF3B1F", 0.25f, 0.28f, emissionHex: "#FF4522", emissionIntensity: 1.6f);
            Material matTracer = CreateURPMaterial("Assets/Materials/Mat_Laser_Tracer.mat", "#A0E0EF", 0.0f, 0.1f, emissionHex: "#60C1E5", emissionIntensity: 6.0f);

            // 2. Build Arena Floor & Perimeter Walls
            GameObject arenaRoot = new GameObject("FPS_Arena");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Arena_Floor";
            floor.transform.SetParent(arenaRoot.transform);
            floor.transform.localScale = new Vector3(4.0f, 1.0f, 4.0f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = matFloor;

            CreateWall(arenaRoot.transform, "Wall_North", new Vector3(0, 2.0f, 20.0f), new Vector3(40.0f, 4.0f, 1.0f), matWall);
            CreateWall(arenaRoot.transform, "Wall_South", new Vector3(0, 2.0f, -20.0f), new Vector3(40.0f, 4.0f, 1.0f), matWall);
            CreateWall(arenaRoot.transform, "Wall_East", new Vector3(20.0f, 2.0f, 0), new Vector3(1.0f, 4.0f, 40.0f), matWall);
            CreateWall(arenaRoot.transform, "Wall_West", new Vector3(-20.0f, 2.0f, 0), new Vector3(1.0f, 4.0f, 40.0f), matWall);

            // Sci-Fi Cover Pillars with Neon Trim (+50% / +70% Arena Polish)
            Vector3[] pillarPositions = {
                new Vector3(-7.0f, 1.5f, -2.0f),
                new Vector3(7.0f, 1.5f, -2.0f),
                new Vector3(-10.0f, 2.0f, 8.0f),
                new Vector3(10.0f, 2.0f, 8.0f),
                new Vector3(0.0f, 1.2f, 4.0f),
            };
            for (int p = 0; p < pillarPositions.Length; p++)
            {
                Vector3 pos = pillarPositions[p];
                CreateWall(arenaRoot.transform, $"Cover_Pillar_{p + 1}", pos, new Vector3(1.8f, pos.y * 2.0f, 1.8f), matNavy);
                CreateWall(arenaRoot.transform, $"Cover_Trim_{p + 1}", new Vector3(pos.x, pos.y * 2.0f - 0.15f, pos.z), new Vector3(1.9f, 0.12f, 1.9f), matNeon);
            }

            // Global URP Post-Processing Volume (Bloom + ACES Tonemapping + Vignette)
            GameObject volObj = new GameObject("Global_PostProcess_Volume");
            Volume vol = volObj.AddComponent<Volume>();
            vol.isGlobal = true;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, "Assets/Materials/FPS_PostProcessProfile.asset");
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 0.65f;
            bloom.scatter.value = 0.6f;
            Tonemapping tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.ACES;
            Vignette vig = profile.Add<Vignette>(true);
            vig.intensity.value = 0.22f;
            vol.sharedProfile = profile;

            // 3. Setup FPS Player & Camera
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0.0f, 1.0f, -10.0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }
            mainCam.transform.SetParent(player.transform);
            mainCam.transform.localPosition = new Vector3(0.0f, 0.7f, 0.0f);
            mainCam.transform.localRotation = Quaternion.identity;
            mainCam.nearClipPlane = 0.03f;
            mainCam.fieldOfView = 62.0f;
            var camData = mainCam.GetUniversalAdditionalCameraData();
            if (camData != null) camData.renderPostProcessing = true;

            var fpsController = player.AddComponent<FPSPlayerController>();
            fpsController.cameraTransform = mainCam.transform;

            // 4. Instantiate Rifle FBX & Auto-Orient Viewmodel
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/rifle_futurista_oneil.fbx");
            GameObject rifleRoot = new GameObject("Rifle_Viewmodel");
            rifleRoot.transform.SetParent(mainCam.transform);
            rifleRoot.transform.localPosition = new Vector3(0.24f, -0.20f, 0.44f);
            rifleRoot.transform.localRotation = Quaternion.identity;

            Transform muzzleTransform = null;
            if (fbxPrefab != null)
            {
                GameObject rifleInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
                rifleInstance.name = "Rifle_ONeil_Model";
                rifleInstance.transform.SetParent(rifleRoot.transform);
                rifleInstance.transform.localPosition = Vector3.zero;
                rifleInstance.transform.localRotation = Quaternion.identity;

                // Map Blender material names to URP palette materials
                foreach (var mr in rifleInstance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = mr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string mname = mats[i] != null ? mats[i].name : "";
                        if (mname.Contains("Blue")) mats[i] = matBlue;
                        else if (mname.Contains("Grey")) mats[i] = matSky;
                        else if (mname.Contains("Neon")) mats[i] = matNeon;
                        else if (mname.Contains("White")) mats[i] = matIce;
                        else if (mname.Contains("Dark") || mname.Contains("Metal")) mats[i] = matNavy;
                    }
                    mr.sharedMaterials = mats;
                }

                // Auto-orient so muzzle points forward (+Z in camera space) and sight points up (+Y)
                Transform muzzleObj = FindChildRecursive(rifleInstance.transform, "Muzzle_Bore_Outer");
                Transform stockObj = FindChildRecursive(rifleInstance.transform, "Stock_Buttpad_Dark");
                Transform sightObj = FindChildRecursive(rifleInstance.transform, "Optic_Hood_Top");
                if (muzzleObj != null && stockObj != null && sightObj != null)
                {
                    Vector3 fwdDir = (muzzleObj.position - stockObj.position).normalized;
                    Vector3 upDir = (sightObj.position - stockObj.position).normalized;
                    rifleInstance.transform.rotation = Quaternion.LookRotation(fwdDir, upDir);
                    // Re-align inverse so model faces rifleRoot.forward (+Z) and up (+Y)
                    Quaternion inv = Quaternion.Inverse(Quaternion.LookRotation(fwdDir, upDir));
                    rifleInstance.transform.localRotation = inv;
                }
                else
                {
                    rifleInstance.transform.localRotation = Quaternion.Euler(0.0f, -90.0f, 0.0f);
                }

                // Create MuzzlePoint at muzzle tip
                GameObject mp = new GameObject("MuzzlePoint");
                mp.transform.SetParent(rifleRoot.transform);
                mp.transform.localPosition = new Vector3(0.0f, 0.02f, 0.48f);
                muzzleTransform = mp.transform;
            }
            else
            {
                Debug.LogWarning("FBX not found at Assets/Models/rifle_futurista_oneil.fbx");
            }

            // Muzzle Flash Light + Laser Tracer
            GameObject flashObj = new GameObject("MuzzleFlashLight");
            flashObj.transform.SetParent(muzzleTransform != null ? muzzleTransform : rifleRoot.transform);
            flashObj.transform.localPosition = Vector3.zero;
            Light flashLight = flashObj.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(0.63f, 0.88f, 0.94f);
            flashLight.range = 6.0f;
            flashLight.intensity = 0.0f;

            GameObject tracerObj = new GameObject("LaserTracer");
            tracerObj.transform.SetParent(rifleRoot.transform);
            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.022f;
            lr.endWidth = 0.010f;
            lr.sharedMaterial = matTracer;
            lr.enabled = false;

            var shooter = player.AddComponent<RifleShooter>();
            shooter.playerCamera = mainCam;
            shooter.rifleViewmodel = rifleRoot.transform;
            shooter.muzzlePoint = muzzleTransform != null ? muzzleTransform : rifleRoot.transform;
            shooter.muzzleFlashLight = flashLight;
            shooter.tracerLine = lr;

            // Reload Disassembly + Laser Recharge Animator
            GameObject reloadLightObj = new GameObject("ReloadRechargeLight");
            reloadLightObj.transform.SetParent(rifleRoot.transform);
            reloadLightObj.transform.localPosition = new Vector3(0.0f, 0.01f, 0.28f);
            Light reloadLight = reloadLightObj.AddComponent<Light>();
            reloadLight.type = LightType.Point;
            reloadLight.color = new Color(0.63f, 0.88f, 0.94f);
            reloadLight.range = 4.5f;
            reloadLight.intensity = 0.0f;

            GameObject rechargeBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rechargeBeam.name = "RechargeLaserCore";
            Object.DestroyImmediate(rechargeBeam.GetComponent<Collider>());
            rechargeBeam.transform.SetParent(rifleRoot.transform);
            rechargeBeam.transform.localPosition = new Vector3(0.0f, 0.01f, 0.28f);
            rechargeBeam.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            rechargeBeam.transform.localScale = new Vector3(0.024f, 0.22f, 0.024f);
            rechargeBeam.GetComponent<MeshRenderer>().sharedMaterial = matNeon;
            rechargeBeam.SetActive(false);

            var reloadAnim = rifleRoot.AddComponent<RifleReloadAnimator>();
            reloadAnim.rifleViewmodel = rifleRoot.transform;
            reloadAnim.reloadLight = reloadLight;
            reloadAnim.rechargeLaserBeam = rechargeBeam.transform;
            shooter.reloadAnimator = reloadAnim;

            // Sci-Fi Audio Sources & Clips (Kenney Sci-Fi Sounds)
            AudioSource sfxSrc = player.AddComponent<AudioSource>();
            sfxSrc.playOnAwake = false;
            sfxSrc.spatialBlend = 0.0f;
            AudioSource beamSrc = player.AddComponent<AudioSource>();
            beamSrc.playOnAwake = false;
            beamSrc.loop = true;
            beamSrc.spatialBlend = 0.0f;
            beamSrc.volume = 0.45f;

            AudioClip clipShot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/KenneySciFi/Audio/laserLarge_000.ogg");
            AudioClip clipBeam = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/KenneySciFi/Audio/forceField_000.ogg");
            AudioClip clipOpen = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/KenneySciFi/Audio/doorOpen_000.ogg");
            AudioClip clipRecharge = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/KenneySciFi/Audio/forceField_002.ogg");
            AudioClip clipClose = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/KenneySciFi/Audio/doorClose_000.ogg");
            AudioClip clipHit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/KenneySciFi/Audio/impactMetal_000.ogg");
            AudioClip clipDestroy = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/KenneySciFi/Audio/explosionCrunch_000.ogg");

            shooter.sfxSource = sfxSrc;
            shooter.beamLoopSource = beamSrc;
            beamSrc.clip = clipBeam;
            shooter.shotClip = clipShot;
            shooter.reloadOpenClip = clipOpen;
            shooter.reloadLaserClip = clipRecharge;
            shooter.reloadCloseClip = clipClose;

            // 5. Spawn Initial Wave of Challenging Target Cubes
            GameObject targetsRoot = new GameObject("Target_Cubes");
            Random.InitState(42);
            for (int i = 0; i < 12; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Target_Cube_W1_{i + 1:00}";
                cube.transform.SetParent(targetsRoot.transform);
                cube.transform.position = new Vector3(
                    Random.Range(-13.0f, 13.0f),
                    Random.Range(1.4f, 4.2f),
                    Random.Range(2.0f, 16.0f)
                );
                cube.transform.localScale = Vector3.one * 0.95f;
                cube.GetComponent<MeshRenderer>().sharedMaterial = matTarget;
                var tc = cube.AddComponent<TargetCube>();
                tc.maxHealth = 2.5f;
                tc.moveSpeed = 2.0f;
                tc.moveRadius = 2.2f;
                tc.pattern = (CubeMovementPattern)(i % 3);
                tc.hitClip = clipHit;
                tc.destroyClip = clipDestroy;
            }

            // 6. Add FPSGameManager
            GameObject gmObj = new GameObject("FPSGameManager");
            var gm = gmObj.AddComponent<FPSGameManager>();
            gm.baseTargetCount = 12;
            gm.targetCubeMaterial = matTarget;
            gm.cubeHitClip = clipHit;
            gm.cubeDestroyClip = clipDestroy;

            // 7. Save Scene & Set Build Settings
            string scenePath = "Assets/Scenes/FPSArena.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("FPS_ARENA_BUILT_SUCCESSFULLY");
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name)) return child;
                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string full = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static Material CreateURPMaterial(string path, string hex, float metallic, float smoothness,
            string emissionHex = null, float emissionIntensity = 0.0f)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            Material mat = new Material(urpLit);
            if (ColorUtility.TryParseHtmlString(hex, out Color col))
            {
                mat.SetColor("_BaseColor", col);
                mat.color = col;
            }
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (!string.IsNullOrEmpty(emissionHex) && ColorUtility.TryParseHtmlString(emissionHex, out Color emCol))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emCol * emissionIntensity);
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
