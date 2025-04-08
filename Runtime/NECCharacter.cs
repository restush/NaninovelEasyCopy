namespace AmoyFeels.EasyCopy
{
#if UNITY_EDITOR

    using Naninovel;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor;

    using System.Reflection;
    using System.Linq;
    using UnityEditor.UIElements;

    public class NECCharacter : NaninovelEasyCopy
    {
        private TransitionalRenderer _renderer;
        public TransitionalRenderer Renderer
        {
            get
            {
                if (_renderer == null)
                    _renderer = TargetObj.GetComponentInChildren<TransitionalRenderer>();
                return _renderer;
            }
        }

        private ICharacterActor _actor;

        public ICharacterActor Actor
        {
            get
            {
                _actor ??= Engine.GetService<ICharacterManager>().GetActor(ActorName);
                return _actor;
            }
        }

        public string ActorName => TargetObj.gameObject.name;




        public override void CreateInspector(VisualElement rootVisualElement)
        {
            var scaleField = new FloatField("Scale");
            rootVisualElement.Add(scaleField);

            var necCharacter = this;
            // apply target object transform to the necCharacter
            necCharacter.transform.position = necCharacter.TargetObj.position;
            necCharacter.transform.localScale = necCharacter.TargetObj.localScale;
            necCharacter.transform.rotation = necCharacter.TargetObj.rotation;

            // create command fields to display the command
            var charPosText = CreateCommandField("Character Pos", rootVisualElement);
            var charRotation = CreateCommandField("Character Rotation", rootVisualElement);
            var charScaleText = CreateCommandField("Character Scale", rootVisualElement);
            var charAllText = CreateCommandField("Character All", rootVisualElement);

            bool EnableCommandPrefix() => EditorPrefs.GetBool("NaninovelEasyCopyEditor_CommandPrefix", true);

            RefreshCommandText(necCharacter);

            AddCommandPrefix(rootVisualElement, () => RefreshCommandText(necCharacter));

            var toggleZ = new Toggle("Enable Z");
            toggleZ.RegisterValueChangedCallback((evt) =>
            {
                EditorPrefs.SetBool("NaninovelEasyCopyEditor_EnableZ", evt.newValue);
                RefreshCommandText(necCharacter);

            });

            toggleZ.value = EditorPrefs.GetBool("NaninovelEasyCopyEditor_EnableZ", false);
            rootVisualElement.Add(toggleZ);

            var visibleToggle = new Toggle("Visible");
            visibleToggle.RegisterValueChangedCallback((evt) =>
            {
                necCharacter.Actor.Visible = evt.newValue;
                RefreshCommandText(necCharacter);
            });

            visibleToggle.value = necCharacter.Actor.Visible;
            rootVisualElement.Add(visibleToggle);

            AppearanceDropdown(necCharacter);


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
                //if (currentAppearances.Count == 0)
                //{
                //    Debug.LogWarning($"No appearances found for {necCharacter.ActorName}");
                //    return;
                //}
                var charaIDs = new DropdownField("Character ID", currentAppearances, 0);
                charaIDs.RegisterValueChangedCallback(evt =>
                {
                    var newApperance = evt.newValue.Contains('.') ? evt.newValue.GetAfter(".") : null;
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

                if (characters.Contains(actor))
                    charaIDs.SetValueWithoutNotify(characters[characters.IndexOf(actor)]);

                rootVisualElement.Add(charaIDs);
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

        public override void OnNaninovelInitializeFinish()
        {
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

        }

        public override Texture GetIcon()
        {
            return EditorGUIUtility.IconContent("Sprite Icon").image;
        }
    }
#endif 
}