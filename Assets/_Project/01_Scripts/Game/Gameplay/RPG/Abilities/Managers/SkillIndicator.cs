using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Managers
{
    public class SkillIndicator : MonoBehaviour
    {
        [SerializeField] private LineRenderer _rangeRenderer;
        [SerializeField] private LineRenderer _areaRenderer;
        [SerializeField] private LineRenderer _directionalRenderer;
        
        [SerializeField] private LayerMask groundLayer;

        private Material _indicatorMaterial;
        private int _circleSegments = 50;

        private void Awake()
        {
            // Setup material for vertex colors (prevents magenta/purple)
            Shader unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader == null) unlitShader = Shader.Find("UI/Default");
            _indicatorMaterial = new Material(unlitShader);

            SetupLineRenderer(_rangeRenderer, 0.06f);
            SetupLineRenderer(_areaRenderer, 0.08f);
            SetupLineRenderer(_directionalRenderer, 0.12f);
        }

        private void SetupLineRenderer(LineRenderer lr, float width)
        {
            if (lr == null) return;
            lr.sharedMaterial = _indicatorMaterial;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }



        public void UpdateRangeIndicator(Vector3 center, float range, Color color)
        {
            if (_rangeRenderer == null) return;

            _rangeRenderer.gameObject.SetActive(true);
            _rangeRenderer.startColor = color;
            _rangeRenderer.endColor = color;
            _rangeRenderer.loop = true;
            _rangeRenderer.positionCount = _circleSegments;

            Vector3[] points = new Vector3[_circleSegments];
            for (int i = 0; i < _circleSegments; i++)
            {
                float angle = i * 2f * Mathf.PI / _circleSegments;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range);
                points[i] = GetGroundPoint(center + offset);
            }

            _rangeRenderer.SetPositions(points);
        }

        public void UpdateAreaIndicator(Vector3 center, float radius, Color color)
        {
            if (_areaRenderer == null) return;

            _areaRenderer.gameObject.SetActive(true);
            _areaRenderer.startColor = color;
            _areaRenderer.endColor = color;
            _areaRenderer.loop = true;
            _areaRenderer.positionCount = _circleSegments;

            Vector3[] points = new Vector3[_circleSegments];
            for (int i = 0; i < _circleSegments; i++)
            {
                float angle = i * 2f * Mathf.PI / _circleSegments;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                points[i] = GetGroundPoint(center + offset);
            }

            _areaRenderer.SetPositions(points);
        }

        public void UpdateDirectionalIndicator(Vector3 start, Vector3 direction, float length, Color color)
        {
            if (_directionalRenderer == null) return;

            _directionalRenderer.gameObject.SetActive(true);
            _directionalRenderer.startColor = color;
            _directionalRenderer.endColor = color;
            _directionalRenderer.loop = false;
            _directionalRenderer.positionCount = 5;

            direction.y = 0;
            direction.Normalize();

            Vector3 end = start + direction * length;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

            Vector3 leftWing = end - direction * 1.5f + right * 0.7f;
            Vector3 rightWing = end - direction * 1.5f - right * 0.7f;

            Vector3[] points = new Vector3[5];
            points[0] = GetGroundPoint(start);
            points[1] = GetGroundPoint(end);
            points[2] = GetGroundPoint(leftWing);
            points[3] = GetGroundPoint(end);
            points[4] = GetGroundPoint(rightWing);

            _directionalRenderer.SetPositions(points);
        }

        public void HideAll()
        {
            if (_rangeRenderer != null) _rangeRenderer.gameObject.SetActive(false);
            if (_areaRenderer != null) _areaRenderer.gameObject.SetActive(false);
            if (_directionalRenderer != null) _directionalRenderer.gameObject.SetActive(false);
        }

        private Vector3 GetGroundPoint(Vector3 rawPoint)
        {
            Vector3 origin = rawPoint + Vector3.up * 5f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 15f, groundLayer);
            
            float bestY = rawPoint.y;
            float minDistance = float.MaxValue;
            bool hitGround = false;
            
            foreach (var hit in hits)
            {
                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    bestY = hit.point.y;
                    hitGround = true;
                }
            }
            
            if (hitGround)
            {
                return new Vector3(rawPoint.x, bestY + 0.05f, rawPoint.z);
            }
            
            return rawPoint;
        }

        private void OnDestroy()
        {
            if (_indicatorMaterial != null)
            {
                Destroy(_indicatorMaterial);
            }
        }
    }
}
