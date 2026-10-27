using UnityEngine;

namespace TrapTheCat.Presentation
{
    public class BackgroundFitter : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private void LateUpdate()
        {
            if (_camera == null || _spriteRenderer == null || _spriteRenderer.sprite == null || !_camera.orthographic)
                return;

            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

            float scale = Mathf.Max(width / spriteSize.x, height / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);

            Vector3 center = _camera.ViewportToWorldPoint(
                new Vector3(0.5f, 0.5f, -_camera.transform.position.z));
            transform.position = new Vector3(center.x, center.y, 0f);
        }
    }
}
