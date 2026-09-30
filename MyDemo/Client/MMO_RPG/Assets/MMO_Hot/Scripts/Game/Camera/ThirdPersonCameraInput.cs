using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MMO_Hot.Game.Camera
{
    /// <summary>用鼠标移动旋转第三人称镜头，并处理滚轮缩放。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineOrbitalFollow), typeof(CinemachineRotationComposer))]
    public sealed class ThirdPersonCameraInput : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference zoomAction;

        [Header("镜头")]
        [SerializeField, Min(0f)] private float horizontalSensitivity = 0.2f;
        [SerializeField, Min(0f)] private float verticalSensitivity = 0.2f;
        [SerializeField] private bool invertVertical;
        [SerializeField, Min(0.1f)] private float minCameraDistance = 2f;
        [SerializeField, Min(0.1f)] private float maxCameraDistance = 12f;
        [SerializeField, Min(0f)] private float zoomSensitivity = 0.01f;

        private CinemachineOrbitalFollow orbitalFollow;
        private InputAction look;
        private InputAction zoom;

        /// <summary>获取 Cinemachine 环绕组件，并复制输入 Action。</summary>
        private void Awake()
        {
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
            look = CloneAction(lookAction);
            zoom = CloneAction(zoomAction);
        }

        /// <summary>启用本组件持有的输入副本。</summary>
        private void OnEnable()
        {
            look?.Enable();
            zoom?.Enable();
        }

        /// <summary>鼠标移动时旋转镜头，滚轮调整镜头距离。</summary>
        private void Update()
        {
            if (look != null)
                RotateCamera(look.ReadValue<Vector2>());

            if (zoom != null)
                ZoomCamera(zoom.ReadValue<float>());
        }


        /// <summary>按鼠标位移更新 Cinemachine 的环绕水平角和俯仰角。</summary>
        private void RotateCamera(Vector2 mouseDelta)
        {
            //水平
            var horizontal = orbitalFollow.HorizontalAxis;
            horizontal.Value = horizontal.ClampValue(horizontal.Value + mouseDelta.x * horizontalSensitivity);
            orbitalFollow.HorizontalAxis = horizontal;

            var vertical = orbitalFollow.VerticalAxis;
            float verticalInput = invertVertical ? -mouseDelta.y : mouseDelta.y;
            vertical.Value = vertical.ClampValue(vertical.Value + verticalInput * verticalSensitivity);
            orbitalFollow.VerticalAxis = vertical;
        }

        //滚轮控制距离
        private void ZoomCamera(float scroll)
        {
            if (Mathf.Abs(scroll) < 0.001f)
                return;

            orbitalFollow.Radius = Mathf.Clamp(
                orbitalFollow.Radius - scroll * zoomSensitivity,
                minCameraDistance,
                maxCameraDistance);
        }


        /// <summary>从 Input Action 引用创建独立副本，避免修改共享输入资产。</summary>
        private static InputAction CloneAction(InputActionReference actionReference)
        {
            return actionReference != null && actionReference.action != null
                ? actionReference.action.Clone()
                : null;
        }



        private void OnDisable()
        {
            look?.Disable();
            zoom?.Disable();
        }

        private void OnDestroy()
        {
            look?.Dispose();
            zoom?.Dispose();
        }

    }
}
