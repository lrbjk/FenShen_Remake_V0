using UnityEngine;

namespace FenShen.Combat
{
    public class CombatSkillPreviewAnchor : MonoBehaviour
    {
        [InspectorName("预览原点")]
        [SerializeField] private Transform previewOrigin;
        [InspectorName("按朝向镜像")]
        [SerializeField] private bool mirrorByFacing = true;

        public Transform PreviewOrigin
        {
            get { return previewOrigin != null ? previewOrigin : transform; }
        }

        public GameObject PreviewTarget
        {
            get { return PreviewOrigin != null ? PreviewOrigin.gameObject : gameObject; }
        }

        public bool MirrorByFacing
        {
            get { return mirrorByFacing; }
        }

        public bool IsFacingLeft()
        {
            return PreviewOrigin != null && PreviewOrigin.localScale.x < 0f;
        }
    }
}
