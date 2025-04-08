namespace AmoyFeels.EasyCopy
{
#if UNITY_EDITOR

    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor;
    using System.Collections.Generic;
    using UnityEngine.TextCore.Text;
    using Naninovel.Metadata;

    public abstract class NaninovelEasyCopy : MonoBehaviour
    {
        public virtual Transform TargetObj { get; set; }

        public static Actor[] allActor { get; set; }
        public static List<string> characters { get; set; }

        public abstract void CreateInspector(VisualElement rootVisualElement);

        public abstract void OnNaninovelInitializeFinish();
        public virtual Texture GetIcon()
        {
            return null;
        }


        protected static string ToString(float value, bool secondDigit = false)
        {
            if (secondDigit)
                return value.ToString("0.##");
            return value.ToString("0.#");
        }


        protected virtual TextField CreateCommandField(string label, VisualElement root)
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

        protected virtual Toggle AddCommandPrefix(VisualElement root, System.Action cb = null)
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

        protected virtual bool DestroyIf()
        {
            return (TargetObj == null);
        }


        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void Update() { }
        protected virtual void LateUpdate()
        {

            if (DestroyIf())
            {
                Destroy(gameObject);
                return;
            }

        }

        protected virtual void OnDestroy() { }



    }

#endif 
}