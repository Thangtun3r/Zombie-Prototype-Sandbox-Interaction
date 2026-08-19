#if UNITY_EDITOR && FPS_ZOMBIE_LEGACY_PROTOTYPE
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FPSZombiePrototypeBuilder
{
    private const string ScenePath = "Assets/Scenes/FPSZombiePrototype.unity";
    private const string MaterialRoot = "Assets/FPSPrototype/Materials/";

    [MenuItem("Tools/FPS Zombie/Rebuild Dynamic Horde-Control Prototype")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before rebuilding the prototype.");

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (GameObject root in scene.GetRootGameObjects())
            UnityEngine.Object.DestroyImmediate(root);

        PrototypeTuning tuning = AssetDatabase.LoadAssetAtPath<PrototypeTuning>("Assets/FPSPrototype/Config/FPSPrototypeTuning.asset");
        if (tuning == null)
            throw new InvalidOperationException("Missing FPSPrototypeTuning asset.");
        ConfigureTuning(tuning);

        Material ground = LoadMaterial("Ground.mat");
        Material road = LoadMaterial("Road.mat");
        Material shoulder = LoadMaterial("Shoulder.mat");
        Material roadLines = LoadMaterial("RoadLines.mat");
        Material concrete = LoadMaterial("Concrete.mat");
        Material construction = LoadMaterial("Construction.mat");
        Material container = LoadMaterial("Container.mat");
        Material metal = LoadMaterial("Metal.mat");
        Material target = LoadMaterial("ShootTarget.mat");
        Material vehicleMaterial = LoadMaterial("Vehicle.mat");
        Material vehicleAccent = LoadMaterial("VehicleAccent.mat");
        Material pistolMaterial = LoadMaterial("Pistol.mat");
        Material shotgunMaterial = LoadMaterial("Shotgun.mat");
        Material normalZombie = LoadMaterial("ZombieNormal.mat");
        Material runnerZombie = LoadMaterial("ZombieRunner.mat");
        Material tankZombie = LoadMaterial("ZombieTank.mat");
        Material water = GetOrCreateWaterMaterial();

        BuildLighting();
        Transform world = BuildWorld(ground, road, shoulder, roadLines, concrete, construction, container, water);

        GameObject systems = new GameObject("GAME SYSTEMS");
        GameManager gameManager = systems.AddComponent<GameManager>();
        gameManager.audioSource = systems.AddComponent<AudioSource>();
        systems.AddComponent<PrototypeSmokeVerifier>();

        GameObject railRig = new GameObject("PLAYER RAIL RIG - ENCOUNTER CONTROLLED");
        railRig.transform.position = new Vector3(0f, 0.62f, 0f);
        AutoVehicleController rail = railRig.AddComponent<AutoVehicleController>();
        rail.tuning = tuning;
        rail.travelDirection = Vector3.forward;
        rail.completionDistance = 525f;
        BuildVehicleVisual(railRig.transform, vehicleMaterial, vehicleAccent);

        Camera camera = BuildPlayerCamera(railRig.transform, tuning, pistolMaterial, shotgunMaterial, out WeaponController weapons);

        GameObject encounterObject = new GameObject("DYNAMIC ENCOUNTER DIRECTOR - 7 STATES");
        EncounterDirector director = encounterObject.AddComponent<EncounterDirector>();
        director.tuning = tuning;
        director.vehicle = railRig.transform;
        director.rail = rail;
        director.normalMaterial = normalZombie;
        director.runnerMaterial = runnerZombie;
        director.tankMaterial = tankZombie;
        director.spawnOnStart = true;
        director.spawns = BuildEncounterSpawns();

        Transform interactions = new GameObject("ONE-SHOT ENVIRONMENTAL CONTROL").transform;
        CreateWaterInteraction(interactions, "S3 WATER - PUSH PRIORITY", new Vector3(7f, 0f, 176f), Vector3.left, tuning, railRig.transform, metal, target, water);
        CreateWaterInteraction(interactions, "S5 WATER - RETREAT LINE", new Vector3(7f, 0f, 324f), Vector3.left, tuning, railRig.transform, metal, target, water);
        CreateContainerInteraction(interactions, "S6 CONTAINER - BLOCK HORDE", 405f, tuning, railRig.transform, container, construction, target);
        CreateWaterInteraction(interactions, "S7 WATER - DYNAMIC COMBINATION", new Vector3(7f, 0f, 458f), Vector3.left, tuning, railRig.transform, metal, target, water);
        CreateContainerInteraction(interactions, "S7 CONTAINER - DYNAMIC COMBINATION", 482f, tuning, railRig.transform, container, construction, target);

        GameObject hudObject = new GameObject("FPS HUD - FORWARD CONTROL LOOP");
        PrototypeHUD hud = hudObject.AddComponent<PrototypeHUD>();
        hud.tuning = tuning;
        hud.weapons = weapons;
        hud.vehicle = rail;
        hud.encounters = director;

        GameObject balanceObject = new GameObject("REALTIME BALANCE WINDOW [F2]");
        RuntimeBalanceWindow balance = balanceObject.AddComponent<RuntimeBalanceWindow>();
        balance.sourceTuning = tuning;
        balance.useRuntimeClone = true;
        balance.visibleAtStart = false;
        balance.pauseSimulationWhenOpen = true;

        Validate(scene, rail, camera, director);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = railRig;
        SceneView.lastActiveSceneView?.FrameSelected();
        AssetDatabase.SaveAssets();
        Debug.Log("[Rail Prototype Builder] Built 7 dynamic encounters, FORWARD/STOP/BACKWARD rail control, 3 PUSH props, 2 BLOCK props, and 45 zombie spawns.");
    }

    private static void ConfigureTuning(PrototypeTuning tuning)
    {
        tuning.name = "FPSPrototypeTuning";
        tuning.vehicleSpeed = 3f;
        tuning.backwardSpeed = 4.2f;
        tuning.minimumStopDuration = 2f;
        tuning.retreatDistance = 26f;
        tuning.dangerDistance = 12f;
        tuning.releaseDistance = 23f;
        tuning.controlledRemainingCount = 2;
        tuning.encounter2StopPosition = 96f;
        tuning.encounter3StopPosition = 166f;
        tuning.encounter4StartPosition = 230f;
        tuning.encounter5RetreatPosition = 316f;
        tuning.encounter6StopPosition = 386f;
        tuning.encounter7StartPosition = 440f;
        tuning.detectionRadius = 38f;
        tuning.alertDelay = 0.2f;
        tuning.encounterDelay = 0.3f;
        tuning.pursuitGraceTime = 0f;
        tuning.contactDistance = 2f;

        tuning.normal = new PrototypeTuning.ZombieStats { health = 44f, speed = 2.4f, knockbackResistance = 0.05f };
        tuning.runner = new PrototypeTuning.ZombieStats { health = 30f, speed = 4.4f, knockbackResistance = 0f };
        tuning.tank = new PrototypeTuning.ZombieStats { health = 250f, speed = 1.55f, knockbackResistance = 0.62f };

        tuning.pistol = new PrototypeTuning.WeaponStats
        {
            damage = 24f, fireRate = 5.5f, magazineSize = 14, reloadTime = 1.25f,
            range = 95f, hitAssistRadius = 0.22f, angle = 0f, knockback = 1.4f
        };
        tuning.shotgun = new PrototypeTuning.WeaponStats
        {
            damage = 32f, fireRate = 0.85f, magazineSize = 4, reloadTime = 2.65f,
            range = 24f, hitAssistRadius = 0f, angle = 40f, knockback = 16f
        };

        PrototypeTuning.EnvironmentStats environment = tuning.environment;
        environment.waterPushStrength = 12f;
        environment.waterDuration = 5.5f;
        environment.waterSlowMultiplier = 0.22f;
        environment.containerFallTime = 0.75f;
        environment.containerDamage = 130f;
        environment.containerBlockDuration = 16f;
        tuning.environment = environment;
        tuning.showDebugOverlay = true;
        tuning.showZombieLabels = false;
        EditorUtility.SetDirty(tuning);
    }

    private static void BuildLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.9f, 0.78f);
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.34f, 0.43f, 0.52f);
        RenderSettings.ambientEquatorColor = new Color(0.18f, 0.2f, 0.22f);
        RenderSettings.ambientGroundColor = new Color(0.07f, 0.075f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.32f, 0.37f, 0.4f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance = 250f;
    }

    private static Transform BuildWorld(Material ground, Material road, Material shoulder, Material roadLines, Material concrete, Material construction, Material container, Material water)
    {
        Transform world = new GameObject("WORLD - DYNAMIC RAIL SPACES").transform;
        Primitive("Ground", PrimitiveType.Cube, world, new Vector3(0f, -0.45f, 270f), new Vector3(52f, 0.7f, 560f), ground, true);
        Primitive("Road", PrimitiveType.Cube, world, new Vector3(0f, -0.04f, 270f), new Vector3(16f, 0.22f, 560f), road, true);
        Primitive("Left Shoulder", PrimitiveType.Cube, world, new Vector3(-10.5f, 0.02f, 270f), new Vector3(5f, 0.28f, 560f), shoulder, true);
        Primitive("Right Shoulder", PrimitiveType.Cube, world, new Vector3(10.5f, 0.02f, 270f), new Vector3(5f, 0.28f, 560f), shoulder, true);

        for (float z = 8f; z < 540f; z += 12f)
            Primitive("Lane Dash " + z.ToString("000"), PrimitiveType.Cube, world, new Vector3(0f, 0.09f, z), new Vector3(0.24f, 0.035f, 5f), roadLines, false);

        for (float z = 25f; z < 530f; z += 44f)
        {
            float leftHeight = 3.5f + (z % 4f);
            float rightHeight = 4.5f + ((z + 2f) % 4f);
            Primitive("Left Industrial Block " + z, PrimitiveType.Cube, world, new Vector3(-17f, leftHeight * 0.5f, z), new Vector3(8f, leftHeight, 18f), concrete, true);
            Primitive("Right Industrial Block " + z, PrimitiveType.Cube, world, new Vector3(17f, rightHeight * 0.5f, z + 12f), new Vector3(8f, rightHeight, 18f), construction, true);
        }

        Primitive("E2 Narrow Street Left", PrimitiveType.Cube, world, new Vector3(-8.7f, 1.8f, 120f), new Vector3(1.1f, 3.6f, 46f), concrete, true);
        Primitive("E2 Narrow Street Right", PrimitiveType.Cube, world, new Vector3(8.7f, 1.8f, 120f), new Vector3(1.1f, 3.6f, 46f), concrete, true);
        Primitive("E5 Retreat Channel Left", PrimitiveType.Cube, world, new Vector3(-9.1f, 1.3f, 344f), new Vector3(1f, 2.6f, 62f), shoulder, true);
        Primitive("E5 Retreat Channel Right", PrimitiveType.Cube, world, new Vector3(9.1f, 1.3f, 344f), new Vector3(1f, 2.6f, 62f), shoulder, true);
        Primitive("E6 Construction Alley Left", PrimitiveType.Cube, world, new Vector3(-8.4f, 2.2f, 410f), new Vector3(1.2f, 4.4f, 56f), construction, true);
        Primitive("E6 Construction Alley Right", PrimitiveType.Cube, world, new Vector3(8.4f, 2.2f, 410f), new Vector3(1.2f, 4.4f, 56f), construction, true);

        CreateSectionGate(world, 18f, "01  BASIC FORWARD", roadLines);
        CreateSectionGate(world, 84f, "02  STOP + HORDE", construction);
        CreateSectionGate(world, 154f, "03  PUSH + PRIORITY", water);
        CreateSectionGate(world, 224f, "04  FORWARD PRESSURE", roadLines);
        CreateSectionGate(world, 304f, "05  RETREAT", water);
        CreateSectionGate(world, 374f, "06  BLOCK THE HORDE", container);
        CreateSectionGate(world, 434f, "07  DYNAMIC COMBINATION", construction);
        CreateSectionGate(world, 524f, "PROTOTYPE END", roadLines);
        return world;
    }

    private static void CreateSectionGate(Transform parent, float z, string label, Material material)
    {
        Primitive(label + " Left Post", PrimitiveType.Cube, parent, new Vector3(-8.8f, 2f, z), new Vector3(0.35f, 4f, 0.35f), material, false);
        Primitive(label + " Right Post", PrimitiveType.Cube, parent, new Vector3(8.8f, 2f, z), new Vector3(0.35f, 4f, 0.35f), material, false);
        Primitive(label + " Beam", PrimitiveType.Cube, parent, new Vector3(0f, 4f, z), new Vector3(18f, 0.35f, 0.35f), material, false);
        CreateWorldText(label, new Vector3(0f, 4.35f, z - 0.22f), 0.12f, Color.white, parent);
    }

    private static void BuildVehicleVisual(Transform railRig, Material body, Material accent)
    {
        Primitive("Rail Rig Deck", PrimitiveType.Cube, railRig, new Vector3(0f, 0.72f, 0f), new Vector3(3.8f, 0.5f, 5.4f), body, false);
        Primitive("Forward Hood", PrimitiveType.Cube, railRig, new Vector3(0f, 1.05f, 2.35f), new Vector3(3.1f, 0.42f, 1.6f), accent, false);
        Primitive("Left Safety Rail", PrimitiveType.Cube, railRig, new Vector3(-1.75f, 1.35f, 0f), new Vector3(0.12f, 0.8f, 5f), accent, false);
        Primitive("Right Safety Rail", PrimitiveType.Cube, railRig, new Vector3(1.75f, 1.35f, 0f), new Vector3(0.12f, 0.8f, 5f), accent, false);
    }

    private static Camera BuildPlayerCamera(Transform railRig, PrototypeTuning tuning, Material pistolMaterial, Material shotgunMaterial, out WeaponController controller)
    {
        GameObject cameraObject = new GameObject("Forward First Person Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(railRig, false);
        cameraObject.transform.localPosition = new Vector3(0f, 2.18f, 0.55f);
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 72f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 420f;
        cameraObject.AddComponent<AudioListener>();
        FirstPersonLook look = cameraObject.AddComponent<FirstPersonLook>();

        controller = cameraObject.AddComponent<WeaponController>();
        PistolWeapon pistol = cameraObject.AddComponent<PistolWeapon>();
        ShotgunWeapon shotgun = cameraObject.AddComponent<ShotgunWeapon>();
        GameObject pistolRoot = BuildPistolViewmodel(cameraObject.transform, pistolMaterial);
        GameObject shotgunRoot = BuildShotgunViewmodel(cameraObject.transform, shotgunMaterial);

        controller.aimCamera = camera;
        controller.vehicle = railRig;
        controller.cameraLook = look;
        controller.pistol = pistol;
        controller.shotgun = shotgun;
        controller.startingWeapon = WeaponKind.Pistol;
        pistol.owner = controller;
        pistol.tuning = tuning;
        pistol.visualRoot = pistolRoot;
        shotgun.owner = controller;
        shotgun.tuning = tuning;
        shotgun.visualRoot = shotgunRoot;
        return camera;
    }

    private static GameObject BuildPistolViewmodel(Transform camera, Material material)
    {
        GameObject root = new GameObject("Pistol Viewmodel");
        root.transform.SetParent(camera, false);
        root.transform.localPosition = new Vector3(0.34f, -0.27f, 0.72f);
        Primitive("Pistol Slide", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.05f, 0.12f), new Vector3(0.22f, 0.16f, 0.65f), material, false);
        GameObject grip = Primitive("Pistol Grip", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.2f, -0.02f), new Vector3(0.18f, 0.38f, 0.2f), material, false);
        grip.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        return root;
    }

    private static GameObject BuildShotgunViewmodel(Transform camera, Material material)
    {
        GameObject root = new GameObject("Shotgun Viewmodel");
        root.transform.SetParent(camera, false);
        root.transform.localPosition = new Vector3(0.28f, -0.34f, 0.92f);
        Primitive("Shotgun Barrel", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.03f, 0.35f), new Vector3(0.2f, 0.18f, 1.5f), material, false);
        Primitive("Shotgun Pump", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.05f, 0.16f), new Vector3(0.32f, 0.26f, 0.48f), material, false);
        Primitive("Shotgun Stock", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.08f, -0.52f), new Vector3(0.28f, 0.3f, 0.62f), material, false);
        return root;
    }

    private static List<EncounterSpawn> BuildEncounterSpawns()
    {
        List<EncounterSpawn> spawns = new List<EncounterSpawn>();
        Add(spawns, "01 BASIC FORWARD", ZombieType.Normal, -2.8f, 54f);
        Add(spawns, "01 BASIC FORWARD", ZombieType.Normal, 0.3f, 59f);
        Add(spawns, "01 BASIC FORWARD", ZombieType.Normal, 3f, 64f);

        float[] stopX = { -4.8f, -3.2f, -1.6f, 0f, 1.6f, 3.2f, 4.8f };
        for (int i = 0; i < stopX.Length; i++) Add(spawns, "02 STOP HORDE", ZombieType.Normal, stopX[i], 120f + (i % 3) * 4f);

        float[] pushX = { -4.4f, -2.6f, -0.8f, 1f, 2.8f, 4.5f };
        for (int i = 0; i < pushX.Length; i++) Add(spawns, "03 PUSH PRIORITY", ZombieType.Normal, pushX[i], 193f + (i % 3) * 3f);
        Add(spawns, "03 PUSH PRIORITY", ZombieType.Tank, 0.2f, 202f);

        Add(spawns, "04 FORWARD PRESSURE", ZombieType.Normal, -4f, 268f);
        Add(spawns, "04 FORWARD PRESSURE", ZombieType.Normal, -1.5f, 273f);
        Add(spawns, "04 FORWARD PRESSURE", ZombieType.Normal, 1.3f, 278f);
        Add(spawns, "04 FORWARD PRESSURE", ZombieType.Normal, 4f, 283f);
        Add(spawns, "04 FORWARD PRESSURE", ZombieType.Runner, -0.4f, 264f);

        float[] retreatX = { -5f, -3.6f, -2.1f, -0.7f, 0.8f, 2.2f, 3.7f, 5f };
        for (int i = 0; i < retreatX.Length; i++) Add(spawns, "05 RETREAT", ZombieType.Normal, retreatX[i], 348f + (i % 4) * 3.5f);

        float[] blockX = { -4.8f, -3.2f, -1.6f, 0f, 1.6f, 3.2f, 4.8f };
        for (int i = 0; i < blockX.Length; i++) Add(spawns, "06 BLOCK HORDE", ZombieType.Normal, blockX[i], 416f + (i % 3) * 4f);

        Add(spawns, "07 DYNAMIC COMBINATION", ZombieType.Normal, -4.6f, 474f);
        Add(spawns, "07 DYNAMIC COMBINATION", ZombieType.Normal, -2.8f, 478f);
        Add(spawns, "07 DYNAMIC COMBINATION", ZombieType.Normal, -1f, 482f);
        Add(spawns, "07 DYNAMIC COMBINATION", ZombieType.Normal, 1f, 486f);
        Add(spawns, "07 DYNAMIC COMBINATION", ZombieType.Normal, 2.9f, 490f);
        Add(spawns, "07 DYNAMIC COMBINATION", ZombieType.Normal, 4.6f, 494f);
        Add(spawns, "07 DYNAMIC COMBINATION", ZombieType.Runner, -0.8f, 470f);
        Add(spawns, "07 DYNAMIC COMBINATION", ZombieType.Tank, 1.5f, 501f);
        return spawns;
    }

    private static void Add(List<EncounterSpawn> list, string zone, ZombieType type, float x, float z)
    {
        list.Add(new EncounterSpawn
        {
            zone = zone,
            type = type,
            worldPosition = new Vector3(x, 0.2f, z),
            facingYaw = 180f
        });
    }

    private static void CreateWaterInteraction(Transform parent, string name, Vector3 position, Vector3 pushDirection, PrototypeTuning tuning, Transform vehicle, Material pipeMaterial, Material targetMaterial, Material waterMaterial)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        WaterPushInteractable interaction = root.AddComponent<WaterPushInteractable>();
        interaction.tuning = tuning;
        interaction.vehicle = vehicle;
        interaction.pushDirection = pushDirection;

        Primitive("Hydrant Base", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.85f, 0.6f, 0.85f), pipeMaterial, false);
        Primitive("Pressurized Pipe", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 1.7f, 0f), new Vector3(0.45f, 1.2f, 0.45f), pipeMaterial, false);
        GameObject nozzle = Primitive("Directional Nozzle", PrimitiveType.Cylinder, root.transform, new Vector3(-0.75f, 1.85f, 0f), new Vector3(0.28f, 1.1f, 0.28f), pipeMaterial, false);
        nozzle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        GameObject weakPoint = Primitive("RED PRESSURE VALVE", PrimitiveType.Sphere, root.transform, new Vector3(0f, 2.35f, 0f), Vector3.one * 0.72f, targetMaterial, true);
        interaction.weakPoint = weakPoint;

        GameObject effect = new GameObject("Water Control Volume");
        effect.transform.SetParent(root.transform, false);
        effect.transform.localPosition = new Vector3(-7f, 1.5f, 9f);
        BoxCollider area = effect.AddComponent<BoxCollider>();
        area.size = new Vector3(16f, 3f, 20f);
        area.isTrigger = true;
        area.enabled = false;
        interaction.effectArea = area;

        GameObject spray = new GameObject("Directional Water Spray");
        spray.transform.SetParent(root.transform, false);
        for (int i = 0; i < 7; i++)
        {
            GameObject jet = Primitive("Water Jet " + i, PrimitiveType.Cube, spray.transform, new Vector3(-6.6f, 0.65f + (i % 2) * 0.18f, 3f + i * 2f), new Vector3(13.2f, 0.12f, 0.18f), waterMaterial, false);
            jet.transform.localRotation = Quaternion.Euler(0f, 0f, -2f + i * 0.7f);
        }
        spray.SetActive(false);
        interaction.sprayVisual = spray;

        for (int i = 0; i < 3; i++)
            Primitive("Direction Arrow " + i, PrimitiveType.Cube, root.transform, new Vector3(-2.6f - i * 1.7f, 0.2f, 1.1f), new Vector3(1.1f, 0.08f, 0.35f), waterMaterial, false);
        CreateWorldText("PUSH / REDIRECT   ←", position + new Vector3(-1.4f, 3.6f, -0.1f), 0.105f, new Color(0.45f, 0.95f, 1f), parent);
    }

    private static void CreateContainerInteraction(Transform parent, string name, float z, PrototypeTuning tuning, Transform vehicle, Material containerMaterial, Material frameMaterial, Material targetMaterial)
    {
        GameObject frame = new GameObject(name + " FRAME");
        frame.transform.SetParent(parent);
        Primitive("Left Gantry", PrimitiveType.Cube, frame.transform, new Vector3(-8f, 5f, z), new Vector3(0.55f, 10f, 0.55f), frameMaterial, false);
        Primitive("Right Gantry", PrimitiveType.Cube, frame.transform, new Vector3(8f, 5f, z), new Vector3(0.55f, 10f, 0.55f), frameMaterial, false);
        Primitive("Gantry Beam", PrimitiveType.Cube, frame.transform, new Vector3(0f, 9.8f, z), new Vector3(16.5f, 0.55f, 0.75f), frameMaterial, false);
        CreateWorldText("CRUSH / BLOCK", new Vector3(0f, 9.35f, z - 0.45f), 0.12f, Color.white, frame.transform);

        GameObject container = new GameObject(name);
        container.transform.SetParent(parent);
        container.transform.position = new Vector3(0f, 7.2f, z);
        Primitive("Container Body", PrimitiveType.Cube, container.transform, Vector3.zero, new Vector3(12f, 2.5f, 3.8f), containerMaterial, false);
        BoxCollider block = container.AddComponent<BoxCollider>();
        block.size = new Vector3(12f, 2.5f, 3.8f);
        block.enabled = false;
        ContainerInteractable interaction = container.AddComponent<ContainerInteractable>();
        interaction.tuning = tuning;
        interaction.vehicle = vehicle;
        interaction.finalWorldPosition = new Vector3(0f, 1.35f, z);
        interaction.finalWorldEuler = Vector3.zero;
        interaction.blockingCollider = block;
        interaction.crushHalfExtents = new Vector3(6.2f, 2.4f, 3.1f);

        GameObject cable = Primitive("Suspension Cable", PrimitiveType.Cube, container.transform, new Vector3(-4.4f, 2.5f, 0f), new Vector3(0.12f, 3.2f, 0.12f), frameMaterial, false);
        GameObject weakPoint = Primitive("RED RELEASE TARGET", PrimitiveType.Sphere, container.transform, new Vector3(-4.4f, 1.35f, -0.15f), Vector3.one * 0.72f, targetMaterial, true);
        interaction.cable = cable;
        interaction.weakPoint = weakPoint;

        for (int i = -2; i <= 2; i++)
            Primitive("Container Rib " + i, PrimitiveType.Cube, container.transform, new Vector3(i * 1.8f, 0f, -0.52f), new Vector3(0.12f, 2.2f, 0.12f), frameMaterial, false);
    }

    private static GameObject CreateWorldText(string text, Vector3 position, float size, Color color, Transform parent)
    {
        GameObject label = new GameObject(text);
        label.transform.SetParent(parent);
        label.transform.position = position;
        label.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        TextMesh mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = size;
        mesh.fontSize = 72;
        mesh.fontStyle = FontStyle.Bold;
        mesh.color = color;
        return label;
    }

    private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        if (!keepCollider)
        {
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }
        return item;
    }

    private static Material LoadMaterial(string fileName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + fileName);
        if (material == null) throw new InvalidOperationException("Missing material: " + fileName);
        return material;
    }

    private static Material GetOrCreateWaterMaterial()
    {
        const string path = MaterialRoot + "WaterPush.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = "WaterPush" };
        Color cyan = new Color(0.05f, 0.65f, 0.95f, 1f);
        material.color = cyan;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cyan);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", cyan * 1.8f);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Validate(Scene scene, AutoVehicleController rail, Camera camera, EncounterDirector director)
    {
        int sections = director.spawns.Select(spawn => spawn.zone).Distinct().Count();
        if (sections != 7) throw new InvalidOperationException("Expected 7 encounter sections, found " + sections + ".");
        if (director.spawns.Count != 45) throw new InvalidOperationException("Expected 45 tuned zombie spawns, found " + director.spawns.Count + ".");
        if (UnityEngine.Object.FindObjectsByType<WaterPushInteractable>(FindObjectsSortMode.None).Length != 3)
            throw new InvalidOperationException("Expected 3 one-shot water interactions.");
        if (UnityEngine.Object.FindObjectsByType<ContainerInteractable>(FindObjectsSortMode.None).Length != 2)
            throw new InvalidOperationException("Expected 2 crush/block interactions.");
        if (Vector3.Dot(camera.transform.forward, rail.travelDirection.normalized) < 0.98f)
            throw new InvalidOperationException("Camera must primarily face the rail progression direction.");
        if (rail.tuning.backwardSpeed <= 0f || rail.tuning.minimumStopDuration < 0f)
            throw new InvalidOperationException("Dynamic rail movement tuning is invalid.");
        if (scene.path != ScenePath) throw new InvalidOperationException("Wrong scene target.");
    }
}
#endif
