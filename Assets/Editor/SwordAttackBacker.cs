using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class SwordAttackBacker : EditorWindow
    {
        [MenuItem("Tools/Sword Swing Baker")]
        static void Open() => GetWindow<SwordAttackBacker>("Sword Swing Baker");

        public GameObject characterPrefab;
        public AnimationClip clip;
        public int sampleCount = 30;
        public GameObject tipTransform;
        public GameObject baseTransform;

        private Vector3[] _previewTips;
        private Vector3[] _previewBases;
        private bool _showPreview;
        private float _previewScrub;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            characterPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Character Prefab", characterPrefab, typeof(GameObject), false);
            clip = (AnimationClip)EditorGUILayout.ObjectField(
                "Animation Clip", clip, typeof(AnimationClip), false);
            sampleCount = EditorGUILayout.IntSlider("Sample Count", sampleCount, 10, 120);

            tipTransform = (GameObject)EditorGUILayout.ObjectField(
                "Sword Tip", tipTransform, typeof(GameObject), true);
            baseTransform = (GameObject)EditorGUILayout.ObjectField(
                "Sword Base", baseTransform, typeof(GameObject), true);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview (Scene)"))
                    PreviewInScene();

                using (new EditorGUI.DisabledScope(!_showPreview))
                {
                    if (GUILayout.Button("Clear Preview"))
                    {
                        _showPreview = false;
                        SceneView.RepaintAll();
                    }
                }
            }

            if (_showPreview && _previewTips != null)
            {
                EditorGUI.BeginChangeCheck();
                _previewScrub = EditorGUILayout.Slider("Scrub", _previewScrub, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                    SceneView.RepaintAll();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Bake to ScriptableObject"))
                Bake();
        }

        private static string GetRelativePath(Transform child, Transform root)
        {
            string path = child.name;
            var current = child.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private (Transform tip, Transform bse) FindOnInstance(GameObject instance)
        {
            Transform sceneRoot = tipTransform.transform.root;
            string tipPath = GetRelativePath(tipTransform.transform, sceneRoot);
            string basePath = GetRelativePath(baseTransform.transform, sceneRoot);
            return (instance.transform.Find(tipPath), instance.transform.Find(basePath));
        }

        private static GameObject FindAnimatorObject(GameObject root)
        {
            var animator = root.GetComponentInChildren<Animator>();
            return animator ? animator.gameObject : root;
        }

        private bool ValidateFields()
        {
            if (characterPrefab && clip && tipTransform && baseTransform)
                return true;
            Debug.LogError("Заполните все поля: Character Prefab, Animation Clip, Sword Tip, Sword Base.");
            return false;
        }

        private void Bake()
        {
            if (!ValidateFields()) return;

            var instance = Instantiate(characterPrefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var (instTip, instBase) = FindOnInstance(instance);
            if (!instTip || !instBase)
            {
                Debug.LogError("Не удалось найти Tip/Base на копии. Проверьте, что они являются потомками Character Prefab.");
                DestroyImmediate(instance);
                return;
            }

            var animTarget = FindAnimatorObject(instance);
            var root = instance.transform;
            var samples = new SwingAttackSword1h.SwingSample[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float normalizedTime = (float)i / (sampleCount - 1);
                clip.SampleAnimation(animTarget, normalizedTime * clip.length);

                samples[i] = new SwingAttackSword1h.SwingSample
                {
                    time = normalizedTime,
                    bladeBase = root.InverseTransformPoint(instBase.position),
                    bladeTip = root.InverseTransformPoint(instTip.position),
                    bladeRotation = Quaternion.Inverse(root.rotation) * instBase.rotation
                };
            }

            var asset = CreateInstance<SwingAttackSword1h>();
            asset.duration = clip.length;
            asset.samples = samples;

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Baked Swing", clip.name + "_Baked", "asset", "Куда сохранить?");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"Запечено {sampleCount} сэмплов → {path}");
            }

            DestroyImmediate(instance);
        }

        private void PreviewInScene()
        {
            if (!ValidateFields()) return;

            var instance = Instantiate(characterPrefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.hideFlags = HideFlags.HideAndDontSave;

            var (instTip, instBase) = FindOnInstance(instance);
            if (!instTip || !instBase)
            {
                Debug.LogError("Не удалось найти Tip/Base на копии. Проверьте, что они являются потомками Character Prefab.");
                DestroyImmediate(instance);
                return;
            }

            var animTarget = FindAnimatorObject(instance);
            _previewTips = new Vector3[sampleCount];
            _previewBases = new Vector3[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                clip.SampleAnimation(animTarget, t * clip.length);
                _previewTips[i] = instTip.position;
                _previewBases[i] = instBase.position;
            }

            DestroyImmediate(instance);
            _showPreview = true;
            _previewScrub = 0f;
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showPreview || _previewTips == null || _previewTips.Length < 2)
                return;

            Vector3 offset = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            if (tipTransform)
            {
                Transform sceneRoot = tipTransform.transform.root;
                offset = sceneRoot.position;
                rot = sceneRoot.rotation;
            }

            int count = _previewTips.Length;

            Handles.color = Color.red;
            for (int i = 0; i < count - 1; i++)
                Handles.DrawLine(rot * _previewTips[i] + offset, rot * _previewTips[i + 1] + offset, 2f);

            Handles.color = Color.cyan;
            for (int i = 0; i < count - 1; i++)
                Handles.DrawLine(rot * _previewBases[i] + offset, rot * _previewBases[i + 1] + offset, 2f);

            Handles.color = new Color(1f, 1f, 0f, 0.2f);
            for (int i = 0; i < count; i++)
                Handles.DrawLine(rot * _previewTips[i] + offset, rot * _previewBases[i] + offset);

            float idx = _previewScrub * (count - 1);
            int i0 = Mathf.FloorToInt(idx);
            int i1 = Mathf.Min(i0 + 1, count - 1);
            float lerp = idx - i0;

            Vector3 scrubTip = rot * Vector3.Lerp(_previewTips[i0], _previewTips[i1], lerp) + offset;
            Vector3 scrubBase = rot * Vector3.Lerp(_previewBases[i0], _previewBases[i1], lerp) + offset;

            Handles.color = Color.green;
            Handles.DrawLine(scrubTip, scrubBase, 4f);
            Handles.SphereHandleCap(0, scrubTip, Quaternion.identity, 0.015f, EventType.Repaint);
            Handles.SphereHandleCap(0, scrubBase, Quaternion.identity, 0.015f, EventType.Repaint);

            Handles.color = Color.white;
            Handles.Label(rot * _previewTips[0] + offset, "START");
            Handles.Label(rot * _previewTips[count - 1] + offset, "END");
        }
    }
}
