namespace AmoyFeels.EasyCopy
{
#if UNITY_EDITOR

    using Naninovel;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor;

    using System.Reflection;
    using UnityEditor.UIElements;

    public class NECCamera : NaninovelEasyCopy
    {
        private Camera _camera;

        private static FieldInfo camZoomOrtho;
        private static FieldInfo camZoomFov;

        // make prop if null then get children
        public Camera Camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = TargetObj.GetComponentInChildren<Camera>();
                }
                return _camera;
            }
        }

        protected override void LateUpdate()
        {
            if (TargetObj == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        public override void CreateInspector(VisualElement root)
        {
            var necCam = this;
            var camPos = CreateCommandField("Camera Pos", root);
            var camZoom = CreateCommandField("Camera Zoom", root);
            var camAll = CreateCommandField("Camera All", root);

            AddCommandPrefix(root, () =>
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
            bool targetJustChanged = false;
            necCam.transform.position = necCam.TargetObj.position;

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

                if (targetJustChanged)
                {
                    targetJustChanged = false;




                    return;
                }

                var necTransform = cb.targetObject as Transform;
                necCam.TargetObj.transform.position = necTransform.position;

            });

            posField.TrackSerializedObjectValue(targetTransformSerializedObject, (s) =>
            {

                var transform = s.targetObject as Transform;
                if (transform.position != necCam.transform.position)
                {
                    necCam.transform.position = transform.position;
                    targetJustChanged = true;
                }

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


        static float CameraZoomValue(Camera camera)
        {
            var manager = Engine.GetService<ICameraManager>();
            var Orthographic = manager.Orthographic;

            float result;
            if (Orthographic)
            {
                var orthographicSize = manager.Camera.orthographicSize;

                camZoomOrtho ??= manager.GetType().GetField("initialOrthoSize", BindingFlags.NonPublic | BindingFlags.Instance);
                var initialOrthoSize = (float)camZoomOrtho.GetValue(manager);

                result = Mathf.Clamp(1f - orthographicSize / initialOrthoSize, 0, .99f);
            }
            else
            {
                camZoomFov ??= manager.GetType().GetField("initialFOV", BindingFlags.NonPublic | BindingFlags.Instance);
                var initialFOV = (float)camZoomFov.GetValue(manager);
                var fieldofView = manager.Camera.fieldOfView;
                result = Mathf.Clamp(1f - Mathf.InverseLerp(5f, initialFOV, fieldofView), 0, .99f);
            }

            return result;
        }

        public override void OnNaninovelInitializeFinish()
        {
            var cameraManager = Engine.GetService<ICameraManager>();
            var mainCamera = cameraManager.Camera;
            if (!mainCamera.transform.parent.TryGetComponent<NaninovelEasyCopy>(out var _))
            {
                var newGo = new GameObject("Camera");
                var comp = newGo.AddComponent<NECCamera>();
                comp.TargetObj = mainCamera.transform.parent;

            }
        }

        public override Texture GetIcon()
        {
            return EditorGUIUtility.IconContent("Camera Icon").image;
        }
    }
#endif 
}