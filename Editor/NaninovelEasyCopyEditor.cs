namespace AmoyFeels.EasyCopy
{
    using Naninovel;
    using Naninovel.Metadata;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Unity.Properties;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(NaninovelEasyCopy), true)]
    public class NaninovelEasyCopyEditor : Editor
    {
        private VisualElement root;


        private static FieldInfo camZoomOrtho;
        private static FieldInfo camZoomFov;


        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            root.schedule.Execute(DelayedExecute).ExecuteLater(0);
            return root;
        }

        private void DelayedExecute()
        {
            var rootParent = root.parent.parent;
            rootParent.Q(null, "unity-imgui-container").style.display = DisplayStyle.None;
            var index = rootParent.parent.IndexOf(rootParent);
            var totalChild = rootParent.parent.childCount;

            var nec = this.target as NaninovelEasyCopy;

            NaninovelEasyCopy.allActor ??= MetadataGenerator.GenerateActorsMetadata();
            NaninovelEasyCopy.characters ??= new List<string>();

            var allActor = NaninovelEasyCopy.allActor;
            var characters = NaninovelEasyCopy.characters;

            if (characters == null || characters.Count == 0)
            {
                foreach (var actor in allActor)
                {
                    if (actor.Type != CharactersConfiguration.DefaultPathPrefix)
                        continue;

                    if (!characters.Contains(actor.Id))
                    {
                        characters.Add(actor.Id);
                    }

                    if (actor.Appearances != null && actor.Appearances.Length > 0)
                    {
                        for (var i = 0; i < actor.Appearances.Length; i++)
                        {
                            characters.Add($"{actor.Id}.{actor.Appearances[i]}");
                        }

                    }
                }
            }


            if (!Engine.Initialized) return;
            if (targets.Length > 1)
            {

                foreach (var item in targets)
                {
                    var multiNec = item as NaninovelEasyCopy;

                    InitializeNEC(multiNec, root);
                }
            }
            else
            {
                InitializeNEC(nec, root);

            }

            var container = new VisualElement()
            {
                style =
            {
                flexGrow = 1,
                alignSelf = Align.Center,
                backgroundImage = new StyleBackground()
            },

            };

            var iconName = nec is NECCamera ? "Camera Icon" : "Sprite Icon";

            var editorImage = EditorGUIUtility.IconContent(iconName).image;

            var editorIcon = new Image() { image = editorImage, style = { width = 56, height = 56, marginLeft = 359 } };

            rootParent.parent.Insert(1, container);

            Color backgroundColor = new Color(0.454f, 0.620f, 0.820f);
            Color textColor = Color.white;
            var banner = new VisualElement() { style = { flexGrow = 1 } };
            var guid = "d04326e78a147c54fa08a3efda9b2ea1";
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
                Debug.LogWarning("[Easy Copy] Cannot find Banner image. Reinstall Easy Copy and make sure include original meta file.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            banner.style.backgroundImage = texture;
            banner.style.height = texture.height;
            banner.style.width = texture.width;
            container.Add(banner);
            banner.Add(editorIcon);


            static void InitializeNEC(NaninovelEasyCopy nec, VisualElement root)
            {
                var newRoot = new VisualElement() { style = { flexGrow = 1, marginTop = 10, marginBottom = 10 } };
                var guidStyle = "94582211ac592524eb705ab9ebfdb1ec";
                var assetPathStyle = AssetDatabase.GUIDToAssetPath(guidStyle);
                var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPathStyle);
                newRoot.styleSheets.Add(style);
                newRoot.AddToClassList("container");
                newRoot.EnableInClassList("container", false);

                if (EditorGUIUtility.isProSkin)
                {
                    newRoot.AddToClassList("dark-theme");
                    //Debug.Log("Dark Theme");
                }
                else
                {
                    //Debug.Log("Light Theme");
                    newRoot.AddToClassList("light-theme");
                }

                root.Add(newRoot);
                nec.CreateInspector(newRoot);

                var delay = 0; // in ms

                foreach (var child in newRoot.Children())
                {
                    child.usageHints = UsageHints.DynamicTransform;
                    child.AddToClassList("animate-set-to-left");
                    child.AddToClassList("animate-to-left");
                    child.EnableInClassList("animate-to-left", false);
                    child.schedule.Execute(() =>
                    {
                        child.EnableInClassList("animate-to-left", true);
                        child.EnableInClassList("animate-set-to-left", false);

                    }).StartingIn(delay);
                    delay += 75;
                }
                newRoot.schedule.Execute(() =>
                {
                    newRoot.EnableInClassList("container", true);
                }).StartingIn(delay);
            }
        }



        [UnityEditor.InitializeOnLoadMethod]
        static void InitOnLoad()
        {
            EditorApplication.hierarchyWindowItemOnGUI += EditorApplication_hierarchyWindowItemOnGUI;
            Engine.OnInitializationFinished += () =>
            {
                var baseType = typeof(NaninovelEasyCopy);

                var derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t));

                //Debug.Log("Derived Types");
                foreach (var item in derivedTypes)
                {
                    //Debug.Log(item.Name);
                    var go = new GameObject(item.Name, item);
                    go.GetComponent<NaninovelEasyCopy>().OnNaninovelInitializeFinish();
                    GameObject.Destroy(go);
                }



            };
        }

        private static void EditorApplication_hierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {

            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj != null && obj.TryGetComponent<NaninovelEasyCopy>(out _)) // Change to your component
            {
                var guid = "c7615d8ed01bbaa49a973c56a05383da";
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var _customIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                Rect iconRect = new Rect(selectionRect.xMax - 20, selectionRect.y, 16, 16);

                GUI.DrawTexture(iconRect, _customIcon);

                var compIcon = obj.TryGetComponent<NaninovelEasyCopy>(out var comp);

                if (compIcon)
                {
                    var editorImage = comp.GetIcon();

                    var iconRect2 = new Rect(selectionRect.xMax - 40, selectionRect.y, 16, 16);
                    GUI.DrawTexture(iconRect2, editorImage);

                }



            }
        }

    } 
}