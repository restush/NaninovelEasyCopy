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

[CustomEditor(typeof(NaninovelEasyCopy), true)]
public class NaninovelEasyCopyEditor : Editor
{
    private VisualElement root;

    public static Actor[] allActor { get; private set; }
    public static List<string> characters { get; private set; }

    public override VisualElement CreateInspectorGUI()
    {
        root = new VisualElement();
        root.schedule.Execute(DelayedExecute).ExecuteLater(1);
        return root;
    }

    static string ToString(float value, bool secondDigit = false)
    {
        if (secondDigit)
            return value.ToString("0.##");
        return value.ToString("0.#");
    }

    static string Vector3Command(string prefix, string paramName, Vector3 value)
    {
        string x = value.x != 0 ? value.x.ToString() : "";
        string y = value.y != 0 || (value.x != 0 && value.z != 0) ? value.y.ToString() : "";
        string z = value.z != 0 ? value.z.ToString() : "";

        if (prefix != null)
            return $"{prefix} {paramName}:{x},{y},{z}".TrimEnd(',');
        return $"{paramName}:{x},{y},{z}".TrimEnd(',');
    }

    private void DelayedExecute()
    {
        var rootParent = root.parent.parent;
        rootParent.Q(null, "unity-imgui-container").style.display = DisplayStyle.None;
        var index = rootParent.parent.IndexOf(rootParent);
        var totalChild = rootParent.parent.childCount;

        var target = this.target as NaninovelEasyCopy;

        allActor ??= MetadataGenerator.GenerateActorsMetadata();
        characters ??= new List<string>();

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

        if (target is NECCamera necCam)
        {
            CreateCamera(necCam);
        }

        if (target is NECCharacter necCharacter)
        {
            CreateCharacter(necCharacter);


        }

        var container = new VisualElement()
        {
            style =
            {
                flexGrow = 1,
                alignSelf = Align.Center,
            }
        };

        var iconName = target is NECCamera ? "Camera Icon" : "Sprite Icon";

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
    }

    private void CreateCharacter(NECCharacter necCharacter)
    {
        // apply target object transform to the necCharacter
        necCharacter.transform.position = necCharacter.TargetObj.position;
        necCharacter.transform.localScale = necCharacter.TargetObj.localScale;
        necCharacter.transform.rotation = necCharacter.TargetObj.rotation;

        // create command fields to display the command
        var charPosText = CreateCommandField("Character Pos");
        var charRotation = CreateCommandField("Character Rotation");
        var charScaleText = CreateCommandField("Character Scale");
        var charAllText = CreateCommandField("Character All");

        bool EnableCommandPrefix() => EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);

        RefreshCommandText(necCharacter);

        AddCommandPrefix(() => RefreshCommandText(necCharacter));

        var toggleZ = new Toggle("Enable Z");
        toggleZ.RegisterValueChangedCallback((evt) =>
        {
            EditorPrefs.SetBool("NaninovelEasyCopyEditor_EnableZ", evt.newValue);
            RefreshCommandText(necCharacter);

        });

        toggleZ.value = EditorPrefs.GetBool("NaninovelEasyCopyEditor_EnableZ", false);
        root.Add(toggleZ);

        var visibleToggle = new Toggle("Visible");
        visibleToggle.RegisterValueChangedCallback((evt) =>
        {
            necCharacter.Actor.Visible = evt.newValue;
            RefreshCommandText(necCharacter);
        });

        visibleToggle.value = necCharacter.Actor.Visible;
        root.Add(visibleToggle);

        AppearanceDropdown(necCharacter);

        var scaleField = new FloatField("Scale");
        root.Insert(0, scaleField);

        // match target object values to the fields
        scaleField.SetValueWithoutNotify(necCharacter.TargetObj.localScale.x);

        // create flags to check if the fields just changed
        bool scaleFieldJustChanged = false;
        bool targetTransformJustChanged = false;

        scaleField.RegisterValueChangedCallback((evt) =>
        {
            scaleFieldJustChanged = true;
            necCharacter.transform.localScale = new Vector3(evt.newValue, evt.newValue, evt.newValue);
        });



        // insert the transform fields to the root

        // create serialized objects for the target object, necCharacter and necCharacter transform
        var targetTransformSerializedObject = new SerializedObject(necCharacter.TargetObj);
        var NECTransform = new SerializedObject(necCharacter.transform);

        // track the values based on the serialized objects
        charPosText.TrackSerializedObjectValue(targetTransformSerializedObject, (s) =>
        {
            var transform = s.targetObject as Transform;
            RefreshCommandText(necCharacter);

            if (scaleFieldJustChanged)
                scaleFieldJustChanged = false;
            else
                scaleField.SetValueWithoutNotify(transform.localScale.x);

            if (transform.position != necCharacter.transform.position)
            {
                necCharacter.transform.position = transform.position;
                targetTransformJustChanged = true;
            }

            if (transform.rotation != necCharacter.transform.rotation)
            {
                necCharacter.transform.rotation = transform.rotation;
                targetTransformJustChanged = true;

            }

            if (transform.localScale != necCharacter.transform.localScale)
            {
                necCharacter.transform.localScale = transform.localScale;
                targetTransformJustChanged = true;

            }

        });

        // track the values based on the serialized objects, this is for the necCharacter transform
        // this is for auto mirroring necCharacter transform to the target object
        scaleField.TrackSerializedObjectValue(NECTransform, (s) =>
        {


            if (targetTransformJustChanged)
            {
                targetTransformJustChanged = false;
                return;
            }
            var transform = s.targetObject as Transform;
            necCharacter.TargetObj.transform.localScale = transform.localScale;
            necCharacter.TargetObj.transform.position = transform.position;
            necCharacter.TargetObj.transform.rotation = transform.rotation;



        });




        void RefreshCommandText(NECCharacter necCharacter)
        {
            charPosText.value = CharacterPos(necCharacter.Renderer);
            charRotation.value = CharacterRotation(necCharacter.Renderer);
            charScaleText.value = CharacterScale(necCharacter.Renderer);
            var addSpace = !EnableCommandPrefix() ? " " : "";
            addSpace = !EnableCommandPrefix() ? " " : "";
            var actorName = necCharacter.ActorName;
            if (!StringUtils.IsNullEmptyOrWhiteSpace(necCharacter.Actor.Appearance))
                actorName += "." + necCharacter.Actor.Appearance;

            charAllText.value = CharacterPos(necCharacter.Renderer) + addSpace +
                CharacterScale(necCharacter.Renderer).Replace($"@char {actorName}", "")
                + addSpace + CharacterRotation(necCharacter.Renderer).Replace($"@char {actorName}", "");
        }


        void AppearanceDropdown(NECCharacter necCharacter)
        {
            var currentAppearances = characters.Where(c => c.EqualsFast(necCharacter.ActorName) || c.StartsWith(necCharacter.ActorName + ".")).ToList();
            var charaIDs = new DropdownField("Character ID", currentAppearances, 0);
            charaIDs.RegisterValueChangedCallback(evt =>
            {
                var newApperance = evt.newValue.Contains('.') ? evt.newValue.GetAfter(".") : null;
                Debug.Log(newApperance + " selected");
                if (StringUtils.IsNullEmptyOrWhiteSpace(newApperance))
                    necCharacter.Actor.ChangeAppearance(null, default);
                else
                    necCharacter.Actor.ChangeAppearance(newApperance, default);

                RefreshCommandText(necCharacter);
            });

            var actor = necCharacter.Actor.Id;
            if (!StringUtils.IsNullEmptyOrWhiteSpace(necCharacter.Actor.Appearance))
            {
                actor += "." + necCharacter.Actor.Appearance;
            }

            charaIDs.SetValueWithoutNotify(characters[characters.IndexOf(actor)]);
            root.Add(charaIDs);
        }


        string CharacterPos(TransitionalRenderer renderer)
        {
            var cameraManager = Engine.GetService<ICameraManager>();
            var position = cameraManager.Configuration.WorldToSceneSpace(renderer.transform.position) * 100;
            var commandPrefix = EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);
            var enableZ = EditorPrefs.GetBool("NaninovelEasyCopyEditor_EnableZ", false);
            var actorName = necCharacter.ActorName;
            if (!StringUtils.IsNullEmptyOrWhiteSpace(necCharacter.Actor.Appearance))
                actorName += "." + necCharacter.Actor.Appearance;

            string result = commandPrefix ? $"@char {actorName} pos:" : "pos:";

            var posZ = renderer.transform.position.z;
            result = result +
                $"{(position.x != 0 ? ToString(position.x) : "")}," +
                     $"{(position.y != 0 ? ToString(position.y) : "")}" +
                        (enableZ ? $",{(posZ != 0 ? ToString(posZ) : "")}" : "");

            return result;
        }

        string CharacterScale(TransitionalRenderer renderer)
        {
            var scale = renderer.transform.localScale;
            var commandPrefix = EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);

            var actorName = necCharacter.ActorName;
            if (!StringUtils.IsNullEmptyOrWhiteSpace(necCharacter.Actor.Appearance))
                actorName += "." + necCharacter.Actor.Appearance;


            string result = commandPrefix ? $"@char {actorName} scale:" : "scale:";

            result = result + ToString(Mathf.Min(scale.x, scale.y));
            return result;
        }

        string CharacterRotation(TransitionalRenderer renderer)
        {
            var rotation = renderer.transform.rotation.eulerAngles;
            var commandPrefix = EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);
            var actorName = necCharacter.ActorName;
            if (!StringUtils.IsNullEmptyOrWhiteSpace(necCharacter.Actor.Appearance))
                actorName += "." + necCharacter.Actor.Appearance;

            string result = commandPrefix ? $"@char {actorName} rot:" : "rot:";

            result = result +
                $"{(rotation.x != 0 ? ToString(rotation.x) : "")}," +
                     $"{(rotation.y != 0 ? ToString(rotation.y) : "")}," +
                        $"{(rotation.z != 0 ? ToString(rotation.z) : "")}";

            return result;
        }
    }

    private void CreateCamera(NECCamera necCam)
    {
        var camPos = CreateCommandField("Camera Pos");
        var camZoom = CreateCommandField("Camera Zoom");
        var camAll = CreateCommandField("Camera All");

        AddCommandPrefix(() =>
        {
            camPos.value = CameraPos(necCam.TargetObj);
            camZoom.value = CameraZoom(necCam.Camera);
            camAll.value = CameraPos(necCam.TargetObj) + CameraZoom(necCam.Camera).Replace("@camera", "");
        });

        var posField = new Vector3Field("Position");
        var orthoField = new FloatField("Zoom");

        posField.SetValueWithoutNotify(necCam.TargetObj.position);
        orthoField.SetValueWithoutNotify(necCam.Camera.orthographic ? necCam.Camera.orthographicSize : necCam.Camera.fieldOfView);

        bool posFieldJustChanged = false;
        bool zoomFieldJustChanged = false;

        posField.RegisterValueChangedCallback((evt) =>
        {
            posFieldJustChanged = true;
            necCam.transform.position = evt.newValue;
        });

        orthoField.RegisterValueChangedCallback((evt) =>
        {
            zoomFieldJustChanged = true;

            if (necCam.Camera.orthographic)
                necCam.Camera.orthographicSize = evt.newValue;
            else
                necCam.Camera.fieldOfView = evt.newValue;
        });

        root.Insert(0, posField);
        root.Insert(1, orthoField);

        var targetTransformSerializedObject = new SerializedObject(necCam.TargetObj);
        var cameraSerializedObject = new SerializedObject(necCam.TargetObj.GetComponentInChildren<Camera>());
        var NECTransform = new SerializedObject(necCam.transform);

        // cam pos is just for tracking, it could be anything but i choose this because declared first
        camPos.TrackSerializedObjectValue(NECTransform, cb =>
        {
            var necTransform = cb.targetObject as Transform;
            necCam.TargetObj.transform.position = necTransform.position;

        });

        posField.TrackSerializedObjectValue(targetTransformSerializedObject, (s) =>
        {
            var transform = s.targetObject as Transform;
            var camera = necCam.TargetObj.GetComponent<Camera>();
            camPos.value = CameraPos(transform);
            camAll.value = CameraPos(transform) + CameraZoom(camera).Replace("@camera", "");
            if (posFieldJustChanged)
                posFieldJustChanged = false;
            else
                posField.SetValueWithoutNotify(transform.position);

        });

        orthoField.TrackSerializedObjectValue(cameraSerializedObject, (s) =>
        {
            var targetObj = necCam.TargetObj;
            var camera = necCam.Camera;

            var zoomValue = CameraZoomValue(camera);

            camZoom.value = CameraZoomToString(camera, zoomValue);
            var commandPrefix = EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);
            var addSpace = !commandPrefix ? " " : "";
            camAll.value = CameraPos(targetObj) + addSpace + CameraZoom(camera).Replace("@camera", "");

            if (zoomFieldJustChanged)
                zoomFieldJustChanged = false;
            else
            {
                var val = camera.orthographic ? camera.orthographicSize : camera.fieldOfView;
                orthoField.SetValueWithoutNotify(val);
            }
        });

        camPos.value = CameraPos(necCam.TargetObj);
        camZoom.value = CameraZoom(necCam.Camera);
        camAll.value = CameraPos(necCam.TargetObj) + CameraZoom(necCam.Camera).Replace("@camera", "");



        static string CameraPos(Transform transform)
        {
            var commandPrefix = EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);

            string result = commandPrefix ? "@camera offset:" : "offset:";

            var posX = transform.position.x;
            var posY = transform.position.y;
            var posZ = transform.position.z;
            var initial = Engine.GetService<ICameraManager>().Configuration.InitialPosition;

            result += $"{(posX != 0 ? ToString(posX) : "")}," +
                      $"{(posY != 0 ? ToString(posY) : "")}," +
                      $"{(posZ != 0 ? ToString(posZ) : "")}";

            return result;
        }

    }


    private Toggle AddCommandPrefix(System.Action cb = null)
    {
        var toggleCommandPrefix = new Toggle("Command Prefix");
        toggleCommandPrefix.RegisterValueChangedCallback((evt) =>
        {
            EditorPrefs.SetBool("NaninovelEasyCopyEditor_CommandPrefix", evt.newValue);
            cb?.Invoke();
        });

        toggleCommandPrefix.value = EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);

        root.Add(toggleCommandPrefix);
        return toggleCommandPrefix;
    }



    static float CameraZoomValue(Camera camera)
    {
        var manager = Engine.GetService<ICameraManager>();
        var Orthographic = manager.Orthographic;
        var orthographicSize = manager.Camera.orthographicSize;
        var initialOrthoSize = (float)manager.GetType().GetField("initialOrthoSize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
        float result;
        if (Orthographic)
        {
            result = Mathf.Clamp(1f - orthographicSize / initialOrthoSize, 0, .99f);
        }
        else
        {
            var initialFOV = (float)manager.GetType().GetField("initialFOV", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
            var fieldofView = manager.Camera.fieldOfView;
            result = Mathf.Clamp(1f - Mathf.InverseLerp(5f, initialFOV, fieldofView), 0, .99f);
        }

        return result;
    }

    static string CameraZoomToString(Camera camera, float size)
    {
        var commandPrefix = EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);
        string result = commandPrefix ? "@camera zoom:" : "zoom:";
        result += ToString(size, true);
        return result;
    }

    static string CameraZoom(Camera camera)
    {
        var commandPrefix = EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);
        string result = commandPrefix ? "@camera zoom:" : "zoom:";
        var value = CameraZoomValue(camera);
        result += ToString(value, true);
        return result;
    }

    private TextField CreateCommandField(string label)
    {
        var textField = new TextField(label)
        {
            style = { flexGrow = 1 }
        };

        var copyButton = new Button(() =>
        {
            EditorGUIUtility.systemCopyBuffer = textField.value;
            Debug.Log("Copied to clipboard: " + textField.value);
        })
        { text = "Copy" };

        var horizontalContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
        horizontalContainer.Add(textField);
        horizontalContainer.Add(copyButton);
        root.Add(horizontalContainer);
        return textField;
    }

    [UnityEditor.InitializeOnLoadMethod]
    static void InitOnLoad()
    {
        Engine.OnInitializationFinished += () =>
        {

            var cameraManager = Engine.GetService<ICameraManager>();
            var mainCamera = cameraManager.Camera;
            if (!mainCamera.transform.parent.TryGetComponent<NaninovelEasyCopy>(out var _))
            {
                var newGo = new GameObject("Camera");
                var comp = newGo.AddComponent<NECCamera>();
                comp.TargetObj = mainCamera.transform.parent;

            }


            Engine.GetService<ICharacterManager>().OnActorAdded += (id) =>
            {
                var actor = Engine.GetService<ICharacterManager>().GetActor(id);

                if (actor == null) return;

                var type = actor.GetType();
                var gameObjectProperty = type.GetProperty("GameObject", BindingFlags.Public | BindingFlags.Instance);

                if (gameObjectProperty == null || gameObjectProperty.PropertyType != typeof(GameObject))
                {
                    return;
                }

                var go = gameObjectProperty.GetValue(actor) as GameObject;
                if (!go.TryGetComponent<NaninovelEasyCopy>(out var _))
                {
                    var newGo = new GameObject(go.name);
                    var comp = newGo.AddComponent<NECCharacter>();
                    comp.TargetObj = go.transform;
                }
            };


        };
    }
}