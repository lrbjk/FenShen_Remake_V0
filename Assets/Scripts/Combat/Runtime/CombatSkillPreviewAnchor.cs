using UnityEngine;

namespace FenShen.Combat
{
    public class CombatSkillPreviewAnchor : MonoBehaviour
    {
        [SerializeField] private Transform previewOrigin;
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
