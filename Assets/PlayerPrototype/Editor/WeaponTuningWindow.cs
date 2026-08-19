using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlayerPrototype.Editor
{
    public sealed class WeaponTuningWindow : EditorWindow
    {
        private const string WeaponFolder = "Assets/PlayerPrototype/Weapons";
        private readonly List<WeaponProfile> profiles = new List<WeaponProfile>();
        private Vector2 scroll;

        [MenuItem("Tools/Zombie Prototype/Weapon Tuning")]
        private static void Open()
        {
            WeaponTuningWindow window = GetWindow<WeaponTuningWindow>("Weapon Tuning");
            window.minSize = new Vector2(1060f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshProfiles();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Weapon Balance", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
                RefreshProfiles();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "All values are live loadout settings. Fire Rate is shots per second; Spread is the cone half-angle in degrees; Damage is per pellet.",
                MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            foreach (WeaponProfile profile in profiles)
                DrawProfile(profile);
            EditorGUILayout.EndScrollView();

            if (profiles.Count == 0)
                EditorGUILayout.HelpBox("No WeaponProfile assets found in " + WeaponFolder + ".", MessageType.Warning);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Controls: 1 = Pistol, 2 = Shotgun, 3 = AK-47, mouse wheel = switch, R = reload.");
        }

        private static void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            Label("Weapon", 105f);
            Label("Damage", 66f);
            Label("Fire Rate", 66f);
            Label("Pellets", 56f);
            Label("Spread", 62f);
            Label("Knockback", 72f);
            Label("Mag", 48f);
            Label("Reload", 60f);
            Label("Range", 58f);
            Label("Headshot", 68f);
            Label("Recoil", 58f);
            Label("Auto", 42f);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawProfile(WeaponProfile profile)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(profile.DisplayName, GUILayout.Width(105f)))
                Selection.activeObject = profile;
            float damage = EditorGUILayout.FloatField(profile.DamagePerPellet, GUILayout.Width(66f));
            float fireRate = EditorGUILayout.FloatField(profile.FireRate, GUILayout.Width(66f));
            int pellets = EditorGUILayout.IntField(profile.PelletCount, GUILayout.Width(56f));
            float spread = EditorGUILayout.FloatField(profile.SpreadAngle, GUILayout.Width(62f));
            float knockback = EditorGUILayout.FloatField(profile.KnockbackImpulse, GUILayout.Width(72f));
            int magazine = EditorGUILayout.IntField(profile.MagazineSize, GUILayout.Width(48f));
            float reload = EditorGUILayout.FloatField(profile.ReloadDuration, GUILayout.Width(60f));
            float range = EditorGUILayout.FloatField(profile.Range, GUILayout.Width(58f));
            float headshot = EditorGUILayout.FloatField(profile.HeadshotMultiplier, GUILayout.Width(68f));
            float recoil = EditorGUILayout.FloatField(profile.RecoilDistance, GUILayout.Width(58f));
            bool automatic = EditorGUILayout.Toggle(profile.Automatic, GUILayout.Width(42f));
            EditorGUILayout.EndHorizontal();

            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObject(profile, "Tune " + profile.DisplayName);
            profile.DamagePerPellet = damage;
            profile.FireRate = fireRate;
            profile.PelletCount = pellets;
            profile.SpreadAngle = spread;
            profile.KnockbackImpulse = knockback;
            profile.MagazineSize = magazine;
            profile.ReloadDuration = reload;
            profile.Range = range;
            profile.HeadshotMultiplier = headshot;
            profile.RecoilDistance = recoil;
            profile.Automatic = automatic;
            EditorUtility.SetDirty(profile);
        }

        private void RefreshProfiles()
        {
            profiles.Clear();
            profiles.AddRange(
                AssetDatabase.FindAssets("t:WeaponProfile", new[] { WeaponFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<WeaponProfile>)
                    .Where(profile => profile != null)
                    .OrderBy(profile => profile.SortOrder));
            Repaint();
        }

        private static void Label(string text, float width)
        {
            GUILayout.Label(text, GUILayout.Width(width));
        }
    }
}
