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
        /// <summary>视角输入引用，通常读取鼠标每帧的水平和竖直位移。</summary>
        [Header("输入")]
        [SerializeField] private InputActionReference lookAction;
        /// <summary>缩放输入引用，通常读取鼠标滚轮。</summary>
        [SerializeField] private InputActionReference zoomAction;

        /// <summary>水平鼠标输入到 Cinemachine 环绕轴增量的倍率。</summary>
        [Header("镜头")]
        [SerializeField, Min(0f)] private float horizontalSensitivity = 0.2f;
        /// <summary>竖直鼠标输入到 Cinemachine 俯仰轴增量的倍率。</summary>
        [SerializeField, Min(0f)] private float verticalSensitivity = 0.2f;
        /// <summary>是否反转竖直视角输入。</summary>
        [SerializeField] private bool invertVertical;
        /// <summary>相机环绕半径的最小值，单位米。</summary>
        [SerializeField, Min(0.1f)] private float minCameraDistance = 2f;
        /// <summary>相机环绕半径的最大值，单位米。</summary>
        [SerializeField, Min(0.1f)] private float maxCameraDistance = 12f;
        /// <summary>滚轮输入到半径变化的倍率；正滚动会缩短相机距离。</summary>
        [SerializeField, Min(0f)] private float zoomSensitivity = 0.01f;

        /// <summary>Cinemachine 环绕跟随组件，保存水平轴、俯仰轴和半径。</summary>
        private CinemachineOrbitalFollow orbitalFollow;
        /// <summary>本组件独立的视角动作副本，停用后关闭，销毁后释放。</summary>
        private InputAction look;
        /// <summary>本组件独立的滚轮动作副本。</summary>
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
            // horizontal：Cinemachine 水平环绕轴的值类型副本，修改后写回组件。
            var horizontal = orbitalFollow.HorizontalAxis;
            horizontal.Value = horizontal.ClampValue(horizontal.Value + mouseDelta.x * horizontalSensitivity);
            orbitalFollow.HorizontalAxis = horizontal;

            // vertical：Cinemachine 竖直俯仰轴的值类型副本，修改后写回组件。
            var vertical = orbitalFollow.VerticalAxis;
            // verticalInput：根据反转选项处理后的竖直鼠标输入量。
            float verticalInput = invertVertical ? -mouseDelta.y : mouseDelta.y;
            vertical.Value = vertical.ClampValue(vertical.Value + verticalInput * verticalSensitivity);
            orbitalFollow.VerticalAxis = vertical;
        }

        /// <summary>按滚轮输入调整距离，并限制缩放范围。</summary>
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



        /// <summary>停止监听本组件的输入副本。</summary>
        private void OnDisable()
        {
            look?.Disable();
            zoom?.Disable();
        }

        /// <summary>释放输入副本，避免遗留输入监听。</summary>
        private void OnDestroy()
        {
            look?.Dispose();
            zoom?.Dispose();
        }

    }
}
