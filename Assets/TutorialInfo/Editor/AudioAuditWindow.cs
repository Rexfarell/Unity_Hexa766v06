using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

public class AudioAuditWindow : EditorWindow
{
    private Vector2 scroll;

    private string filter = "";

    private bool showSceneSources = true;
    private bool showSerializedReferences = true;
    private bool showProjectClips = true;


    // =========================================================
    // DATA
    // =========================================================

    private class SerializedAudioReference
    {
        public UnityEngine.Object ownerObject;
        public Component component;

        public string objectPath;
        public string componentName;
        public string propertyPath;

        public AudioClip clip;

        public bool isPrefab;
        public string assetPath;
    }


    private List<AudioSource> sceneSources =
        new List<AudioSource>();

    private List<SerializedAudioReference> serializedReferences =
        new List<SerializedAudioReference>();

    private List<AudioClip> projectClips =
        new List<AudioClip>();


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem("Tools/HEXA_766/Audio Audit")]
    public static void OpenWindow()
    {
        GetWindow<AudioAuditWindow>(
            "HEXA_766 Audio Audit"
        );
    }


    private void OnEnable()
    {
        FullScan();
    }


    // =========================================================
    // SCAN
    // =========================================================

    private void FullScan()
    {
        ScanSceneSources();

        serializedReferences.Clear();

        ScanSceneSerializedAudio();
        ScanPrefabSerializedAudio();

        ScanProjectAudioClips();

        Repaint();
    }


    // =========================================================
    // SCENE AUDIO SOURCES
    // =========================================================

    private void ScanSceneSources()
    {
        sceneSources =
            UnityEngine.Object
            .FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
            .Where(source =>
                source != null &&
                source.gameObject.scene.IsValid()
            )
            .OrderBy(source =>
                GetObjectPath(source.transform)
            )
            .ToList();
    }


    // =========================================================
    // SCENE SERIALIZED REFERENCES
    // =========================================================

    private void ScanSceneSerializedAudio()
    {
        Component[] components =
            UnityEngine.Object
            .FindObjectsByType<Component>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        foreach (Component component in components)
        {
            if (component == null)
                continue;

            if (!component.gameObject.scene.IsValid())
                continue;


            ScanComponentForAudioClips(
                component,
                false,
                ""
            );
        }
    }


    // =========================================================
    // PREFAB SERIALIZED REFERENCES
    // =========================================================

    private void ScanPrefabSerializedAudio()
    {
        string[] prefabGuids =
            AssetDatabase.FindAssets(
                "t:Prefab"
            );


        foreach (string guid in prefabGuids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );


            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    assetPath
                );


            if (prefab == null)
                continue;


            Component[] components =
                prefab.GetComponentsInChildren<Component>(
                    true
                );


            foreach (Component component in components)
            {
                if (component == null)
                    continue;


                ScanComponentForAudioClips(
                    component,
                    true,
                    assetPath
                );
            }
        }
    }


    // =========================================================
    // COMPONENT SERIALIZED SCAN
    // =========================================================

    private void ScanComponentForAudioClips(
        Component component,
        bool isPrefab,
        string assetPath
    )
    {
        SerializedObject serializedObject;

        try
        {
            serializedObject =
                new SerializedObject(component);
        }
        catch
        {
            return;
        }


        SerializedProperty property =
            serializedObject.GetIterator();


        bool enterChildren = true;


        while (property.NextVisible(
            enterChildren
        ))
        {
            enterChildren = false;


            if (property.propertyType !=
                SerializedPropertyType.ObjectReference)
            {
                continue;
            }


            AudioClip clip =
                property.objectReferenceValue
                as AudioClip;


            if (clip == null)
                continue;


            serializedReferences.Add(
                new SerializedAudioReference
                {
                    ownerObject =
                        component.gameObject,

                    component =
                        component,

                    objectPath =
                        GetObjectPath(
                            component.transform
                        ),

                    componentName =
                        component.GetType().Name,

                    propertyPath =
                        property.propertyPath,

                    clip =
                        clip,

                    isPrefab =
                        isPrefab,

                    assetPath =
                        assetPath
                }
            );
        }
    }


    // =========================================================
    // PROJECT CLIPS
    // =========================================================

    private void ScanProjectAudioClips()
    {
        projectClips.Clear();


        string[] audioGuids =
            AssetDatabase.FindAssets(
                "t:AudioClip"
            );


        foreach (string guid in audioGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );


            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    path
                );


            if (clip != null)
            {
                projectClips.Add(clip);
            }
        }


        projectClips =
            projectClips
            .OrderBy(clip => clip.name)
            .ToList();
    }


    // =========================================================
    // GUI
    // =========================================================

    private void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "HEXA_766 AUDIO AUDIT",
            EditorStyles.boldLabel
        );


        EditorGUILayout.Space();


        EditorGUILayout.BeginHorizontal();


        filter =
            EditorGUILayout.TextField(
                "Filter",
                filter
            );


        if (GUILayout.Button(
            "FULL SCAN",
            GUILayout.Width(100)
        ))
        {
            FullScan();
        }


        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space();


        EditorGUILayout.LabelField(
            $"Scene AudioSources: {sceneSources.Count}"
        );

        EditorGUILayout.LabelField(
            $"Serialized AudioClip references: {serializedReferences.Count}"
        );

        EditorGUILayout.LabelField(
            $"Project AudioClips: {projectClips.Count}"
        );


        EditorGUILayout.Space();


        scroll =
            EditorGUILayout.BeginScrollView(
                scroll
            );


        DrawSceneAudioSources();

        EditorGUILayout.Space(10);

        DrawSerializedReferences();

        EditorGUILayout.Space(10);

        DrawProjectAudioClips();


        EditorGUILayout.EndScrollView();
    }


    // =========================================================
    // SECTION 1 — AUDIO SOURCES
    // =========================================================

    private void DrawSceneAudioSources()
    {
        showSceneSources =
            EditorGUILayout.Foldout(
                showSceneSources,
                "1. SCENE AUDIO SOURCES",
                true
            );


        if (!showSceneSources)
            return;


        foreach (AudioSource source in sceneSources)
        {
            if (source == null)
                continue;


            string path =
                GetObjectPath(
                    source.transform
                );


            string clipName =
                source.clip != null
                    ? source.clip.name
                    : "<NO DIRECT CLIP>";


            if (!MatchesFilter(
                path,
                clipName,
                "AudioSource"
            ))
            {
                continue;
            }


            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            EditorGUILayout.LabelField(
                path,
                EditorStyles.boldLabel
            );


            EditorGUILayout.LabelField(
                "Direct Clip",
                clipName
            );


            EditorGUILayout.LabelField(
                "Object Active",
                source.gameObject.activeInHierarchy
                    ? "YES"
                    : "NO"
            );


            EditorGUILayout.LabelField(
                "AudioSource Enabled",
                source.enabled
                    ? "YES"
                    : "NO"
            );


            EditorGUILayout.LabelField(
                "Mute",
                source.mute
                    ? "YES"
                    : "NO"
            );


            EditorGUILayout.LabelField(
                "Volume",
                source.volume.ToString("0.00")
            );


            EditorGUILayout.LabelField(
                "Play On Awake",
                source.playOnAwake
                    ? "YES"
                    : "NO"
            );


            EditorGUILayout.LabelField(
                "Loop",
                source.loop
                    ? "YES"
                    : "NO"
            );


            EditorGUILayout.LabelField(
                "Spatial Blend",
                source.spatialBlend.ToString("0.00")
            );


            string mixer =
                source.outputAudioMixerGroup != null
                    ? source.outputAudioMixerGroup.name
                    : "<NONE>";


            EditorGUILayout.LabelField(
                "Mixer Group",
                mixer
            );


            EditorGUILayout.BeginHorizontal();


            if (GUILayout.Button("SELECT"))
            {
                Selection.activeGameObject =
                    source.gameObject;

                EditorGUIUtility.PingObject(
                    source.gameObject
                );
            }


            if (source.clip != null &&
                GUILayout.Button("PING CLIP"))
            {
                Selection.activeObject =
                    source.clip;

                EditorGUIUtility.PingObject(
                    source.clip
                );
            }


            if (source.mute &&
                GUILayout.Button("UNMUTE"))
            {
                Undo.RecordObject(
                    source,
                    "Unmute Audio Source"
                );

                source.mute =
                    false;

                SaveSourceChange(
                    source
                );
            }


            if (!source.enabled &&
                GUILayout.Button("ENABLE"))
            {
                Undo.RecordObject(
                    source,
                    "Enable Audio Source"
                );

                source.enabled =
                    true;

                SaveSourceChange(
                    source
                );
            }


            if (source.volume <= 0.001f &&
                GUILayout.Button("VOLUME = 1"))
            {
                Undo.RecordObject(
                    source,
                    "Restore Audio Volume"
                );

                source.volume =
                    1f;

                SaveSourceChange(
                    source
                );
            }


            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();
        }
    }


    // =========================================================
    // SECTION 2 — SERIALIZED REFERENCES
    // =========================================================

    private void DrawSerializedReferences()
    {
        showSerializedReferences =
            EditorGUILayout.Foldout(
                showSerializedReferences,
                "2. SERIALIZED AUDIO REFERENCES",
                true
            );


        if (!showSerializedReferences)
            return;


        foreach (
            SerializedAudioReference reference
            in serializedReferences
        )
        {
            if (reference == null ||
                reference.clip == null)
            {
                continue;
            }


            if (!MatchesFilter(
                reference.objectPath,
                reference.componentName,
                reference.propertyPath,
                reference.clip.name,
                reference.assetPath
            ))
            {
                continue;
            }


            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            string sourceType =
                reference.isPrefab
                    ? "PREFAB"
                    : "SCENE";


            EditorGUILayout.LabelField(
                $"{sourceType} // {reference.objectPath}",
                EditorStyles.boldLabel
            );


            EditorGUILayout.LabelField(
                "Component",
                reference.componentName
            );


            EditorGUILayout.LabelField(
                "Field",
                reference.propertyPath
            );


            EditorGUILayout.LabelField(
                "Clip",
                reference.clip.name
            );


            if (reference.isPrefab)
            {
                EditorGUILayout.LabelField(
                    "Prefab",
                    reference.assetPath
                );
            }


            EditorGUILayout.BeginHorizontal();


            if (!reference.isPrefab &&
                reference.ownerObject != null &&
                GUILayout.Button("SELECT OBJECT"))
            {
                Selection.activeObject =
                    reference.ownerObject;

                EditorGUIUtility.PingObject(
                    reference.ownerObject
                );
            }


            if (reference.isPrefab &&
                !string.IsNullOrEmpty(
                    reference.assetPath
                ) &&
                GUILayout.Button("PING PREFAB"))
            {
                UnityEngine.Object prefab =
                    AssetDatabase.LoadAssetAtPath<
                        UnityEngine.Object
                    >(
                        reference.assetPath
                    );


                if (prefab != null)
                {
                    Selection.activeObject =
                        prefab;

                    EditorGUIUtility.PingObject(
                        prefab
                    );
                }
            }


            if (GUILayout.Button("PING CLIP"))
            {
                Selection.activeObject =
                    reference.clip;

                EditorGUIUtility.PingObject(
                    reference.clip
                );
            }


            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();
        }
    }


    // =========================================================
    // SECTION 3 — PROJECT AUDIO CLIPS
    // =========================================================

    private void DrawProjectAudioClips()
    {
        showProjectClips =
            EditorGUILayout.Foldout(
                showProjectClips,
                "3. PROJECT AUDIO CLIPS",
                true
            );


        if (!showProjectClips)
            return;


        foreach (AudioClip clip in projectClips)
        {
            if (clip == null)
                continue;


            string assetPath =
                AssetDatabase.GetAssetPath(
                    clip
                );


            int referenceCount =
                serializedReferences.Count(
                    reference =>
                        reference.clip == clip
                );


            int directSourceCount =
                sceneSources.Count(
                    source =>
                        source != null &&
                        source.clip == clip
                );


            int totalKnownReferences =
                referenceCount +
                directSourceCount;


            if (!MatchesFilter(
                clip.name,
                assetPath
            ))
            {
                continue;
            }


            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            EditorGUILayout.LabelField(
                clip.name,
                EditorStyles.boldLabel
            );


            EditorGUILayout.LabelField(
                "Asset",
                assetPath
            );


            EditorGUILayout.LabelField(
                "Length",
                clip.length.ToString("0.00") +
                " sec"
            );


            EditorGUILayout.LabelField(
                "Channels",
                clip.channels.ToString()
            );


            EditorGUILayout.LabelField(
                "Frequency",
                clip.frequency +
                " Hz"
            );


            EditorGUILayout.LabelField(
                "Known Scene/Prefab References",
                totalKnownReferences.ToString()
            );


            if (totalKnownReferences == 0)
            {
                EditorGUILayout.HelpBox(
                    "No serialized scene/prefab reference was found. " +
                    "This does NOT automatically mean the clip is safe to delete.",
                    MessageType.Info
                );
            }


            if (GUILayout.Button("PING CLIP"))
            {
                Selection.activeObject =
                    clip;

                EditorGUIUtility.PingObject(
                    clip
                );
            }


            EditorGUILayout.EndVertical();
        }
    }


    // =========================================================
    // FILTER
    // =========================================================

    private bool MatchesFilter(
        params string[] values
    )
    {
        if (string.IsNullOrWhiteSpace(
            filter
        ))
        {
            return true;
        }


        string search =
            filter.Trim()
            .ToLowerInvariant();


        foreach (string value in values)
        {
            if (string.IsNullOrEmpty(
                value
            ))
            {
                continue;
            }


            if (value
                .ToLowerInvariant()
                .Contains(search))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private static void SaveSourceChange(
        AudioSource source
    )
    {
        EditorUtility.SetDirty(
            source
        );


        if (source.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(
                source.gameObject.scene
            );
        }
    }


    private static string GetObjectPath(
        Transform target
    )
    {
        if (target == null)
            return "<NULL>";


        string path =
            target.name;


        Transform parent =
            target.parent;


        while (parent != null)
        {
            path =
                parent.name +
                "/" +
                path;

            parent =
                parent.parent;
        }


        return path;
    }
}