using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ControlRoom.UI
{
    /// <summary>
    /// 시나리오 진행 순서에 따라 클릭해야 할 UI 버튼 위치를 비간섭적으로 안내하는
    /// 입체 각면 클래식 3D 가이드 화살표(A안) 컴포넌트입니다.
    /// XR 레이캐스트를 방해하지 않도록 Collider를 생성하지 않으며, 화살표가 가리키는 축 방향으로 부드럽게 루프 이동합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScenarioGuideArrow3D : MonoBehaviour
    {
        [Header("표시 및 색상 설정")]
        [SerializeField] private bool mEnableGuideArrow = true;
        [SerializeField] private Color mArrowColor = new Color(1f, 0.8f, 0.2f, 1f); // #FFCC33 골드 옐로우
        [SerializeField, Range(0f, 4f)] private float mEmissionIntensity = 1.35f;

        [Header("크기 및 XR 비가림 배치 설정 (미터 단위)")]
        [SerializeField, Min(0.01f)] private float mArrowLengthMeters = 0.042f;
        [SerializeField, Min(0.002f)] private float mScreenForwardOffsetMeters = 0.015f;
        [SerializeField, Min(0f)] private float mEdgeGapMeters = 0.004f;
        [SerializeField, Range(10f, 75f)] private float mTiltFromScreenDeg = 38f;

        [Header("루프 애니메이션 설정")]
        [SerializeField, Min(0f)] private float mBobAmplitudeMeters = 0.011f;
        [SerializeField, Min(0.1f)] private float mBobFrequencyHz = 1.6f;

        [SerializeField, HideInInspector] private GameObject mVisualRoot;
        [SerializeField, HideInInspector] private MeshFilter mMeshFilter;
        [SerializeField, HideInInspector] private MeshRenderer mMeshRenderer;
        [SerializeField, HideInInspector] private Mesh mOwnedMesh;
        [SerializeField, HideInInspector] private Material mOwnedMaterial;

        private RectTransform mTargetRect;
        private Canvas mTargetCanvas;
        private float mAnimTime;
        private Vector3 mBaseTipWorldPos;
        private Vector3 mPointDirection = Vector3.forward;
        private Vector3 mUpDirection = Vector3.up;
        private bool mHasValidPlacement;

        private readonly Vector3[] mTargetCorners = new Vector3[4];
        private readonly Vector3[] mCanvasCorners = new Vector3[4];

        public bool IsVisible => mVisualRoot != null && mVisualRoot.activeSelf;
        public RectTransform CurrentTargetRect => mTargetRect;
        public Mesh SharedArrowMesh => mMeshFilter != null ? mMeshFilter.sharedMesh : null;
        public Material SharedArrowMaterial => mMeshRenderer != null ? mMeshRenderer.sharedMaterial : null;

        private void Awake()
        {
            ensureVisuals();
        }

        private void OnEnable()
        {
            ensureVisuals();
            updatePlacementAndPose();
        }

        private void OnValidate()
        {
            try
            {
                mArrowLengthMeters = Mathf.Max(0.01f, mArrowLengthMeters);
                mScreenForwardOffsetMeters = Mathf.Max(0.002f, mScreenForwardOffsetMeters);
                mEdgeGapMeters = Mathf.Max(0f, mEdgeGapMeters);
                mBobAmplitudeMeters = Mathf.Max(0f, mBobAmplitudeMeters);
                mBobFrequencyHz = Mathf.Max(0.1f, mBobFrequencyHz);
                applyMaterialProperties();
                updatePlacementAndPose();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ScenarioGuideArrow3D] OnValidate 경고: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (mOwnedMesh != null)
            {
                if (Application.isPlaying) Destroy(mOwnedMesh);
                else DestroyImmediate(mOwnedMesh);
                mOwnedMesh = null;
            }
            if (mOwnedMaterial != null)
            {
                if (Application.isPlaying) Destroy(mOwnedMaterial);
                else DestroyImmediate(mOwnedMaterial);
                mOwnedMaterial = null;
            }
        }

        /// <summary>에디터 빌더에서 생성한 영구 Mesh/Material 에셋을 연결해 프리팹 직렬화 누락을 방지합니다.</summary>
        public void assignSerializedAssets(Mesh meshAsset, Material materialAsset)
        {
            try
            {
                ensureVisuals();
                if (meshAsset != null && mMeshFilter != null)
                {
                    mMeshFilter.sharedMesh = meshAsset;
                }
                if (materialAsset != null && mMeshRenderer != null)
                {
                    mMeshRenderer.sharedMaterial = materialAsset;
                }
                applyMaterialProperties();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioGuideArrow3D] assignSerializedAssets 오류: {ex}");
            }
        }

        /// <summary>가이드 화살표 시각 오브젝트와 A안 각면 메쉬·URP 머티리얼을 초기화합니다.</summary>
        public void ensureVisuals()
        {
            try
            {
                if (mVisualRoot == null)
                {
                    Transform existing = transform.Find("ArrowVisualMesh");
                    if (existing != null)
                    {
                        mVisualRoot = existing.gameObject;
                    }
                    else
                    {
                        mVisualRoot = new GameObject("ArrowVisualMesh");
                        mVisualRoot.transform.SetParent(transform, false);
                        mVisualRoot.transform.localPosition = Vector3.zero;
                        mVisualRoot.transform.localRotation = Quaternion.identity;
                        mVisualRoot.transform.localScale = Vector3.one;
                    }
                }

                // XR 컨트롤러/레이캐스트 간섭 방지를 위해 Collider가 남아있으면 제거
                Collider existingCol = mVisualRoot.GetComponent<Collider>();
                if (existingCol != null)
                {
                    if (Application.isPlaying) Destroy(existingCol);
                    else DestroyImmediate(existingCol);
                }

                mMeshFilter = mVisualRoot.GetComponent<MeshFilter>();
                if (mMeshFilter == null) mMeshFilter = mVisualRoot.AddComponent<MeshFilter>();

                mMeshRenderer = mVisualRoot.GetComponent<MeshRenderer>();
                if (mMeshRenderer == null) mMeshRenderer = mVisualRoot.AddComponent<MeshRenderer>();

                mMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                mMeshRenderer.receiveShadows = false;
                mMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
                mMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                if (mMeshFilter.sharedMesh == null)
                {
                    mOwnedMesh = buildFacetedClassicArrowMesh();
                    mOwnedMesh.hideFlags = HideFlags.DontSave;
                    mMeshFilter.sharedMesh = mOwnedMesh;
                }

                if (mMeshRenderer.sharedMaterial == null)
                {
                    mOwnedMaterial = createArrowMaterial(mArrowColor, mEmissionIntensity);
                    if (mOwnedMaterial != null)
                    {
                        mOwnedMaterial.hideFlags = HideFlags.DontSave;
                        mMeshRenderer.sharedMaterial = mOwnedMaterial;
                    }
                }
                else
                {
                    applyMaterialProperties();
                }

                if (mTargetRect == null) mVisualRoot.SetActive(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioGuideArrow3D] ensureVisuals 오류: {ex}");
            }
        }

        /// <summary>안내할 UI 타겟과 소속 Canvas를 지정합니다. targetRect가 null이면 화살표를 즉시 숨깁니다.</summary>
        public void setTarget(RectTransform targetRect, Canvas monitorCanvas)
        {
            try
            {
                ensureVisuals();
                bool targetChanged = mTargetRect != targetRect || mTargetCanvas != monitorCanvas;
                mTargetRect = targetRect;
                mTargetCanvas = monitorCanvas;
                if (targetChanged) mAnimTime = 0f;

                updatePlacementAndPose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioGuideArrow3D] setTarget 오류: {ex}");
            }
        }

        /// <summary>매 프레임 화살표가 가리키는 방향 축으로 부드럽게 왕복 애니메이션을 갱신합니다.</summary>
        public void tickAnimation(float deltaTime)
        {
            try
            {
                if (!mEnableGuideArrow || mTargetRect == null || mTargetCanvas == null || !mTargetRect.gameObject.activeInHierarchy)
                {
                    if (mVisualRoot != null && mVisualRoot.activeSelf) mVisualRoot.SetActive(false);
                    return;
                }

                if (deltaTime > 0f)
                {
                    mAnimTime += deltaTime;
                }

                updatePlacementAndPose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioGuideArrow3D] tickAnimation 오류: {ex}");
            }
        }

        private void updatePlacementAndPose()
        {
            if (mVisualRoot == null) return;

            if (!mEnableGuideArrow || mTargetRect == null || mTargetCanvas == null || !mTargetRect.gameObject.activeInHierarchy)
            {
                mHasValidPlacement = false;
                mVisualRoot.SetActive(false);
                return;
            }

            computeTargetPlacement();
            if (!mHasValidPlacement)
            {
                mVisualRoot.SetActive(false);
                return;
            }

            if (!mVisualRoot.activeSelf) mVisualRoot.SetActive(true);

            // 화살표 끝점이 가리키는 축(mPointDirection) 방향으로만 왕복 루프
            float phase = 0.5f - 0.5f * Mathf.Cos(mAnimTime * Mathf.PI * 2f * mBobFrequencyHz);
            float pullBack = phase * mBobAmplitudeMeters;
            transform.position = mBaseTipWorldPos - mPointDirection * pullBack;
            transform.rotation = Quaternion.LookRotation(mPointDirection, mUpDirection);
            transform.localScale = Vector3.one * mArrowLengthMeters;
        }

        private void computeTargetPlacement()
        {
            mTargetRect.GetWorldCorners(mTargetCorners);
            Vector3 btnCenter = (mTargetCorners[0] + mTargetCorners[2]) * 0.5f;

            RectTransform canvasRt = mTargetCanvas.GetComponent<RectTransform>();
            Vector3 canvasRight = mTargetCanvas.transform.right.normalized;
            Vector3 canvasUp = mTargetCanvas.transform.up.normalized;
            Vector3 outwardNormal = -mTargetCanvas.transform.forward.normalized;

            float nx = 0.5f;
            float ny = 0.5f;
            if (canvasRt != null)
            {
                canvasRt.GetWorldCorners(mCanvasCorners);
                Vector3 canvasSpanX = mCanvasCorners[3] - mCanvasCorners[0];
                Vector3 canvasSpanY = mCanvasCorners[1] - mCanvasCorners[0];
                float canvasW = canvasSpanX.magnitude;
                float canvasH = canvasSpanY.magnitude;
                if (canvasW > 0.001f && canvasH > 0.001f)
                {
                    canvasRight = canvasSpanX / canvasW;
                    canvasUp = canvasSpanY / canvasH;
                    Vector3 rel = btnCenter - mCanvasCorners[0];
                    nx = Mathf.Clamp01(Vector3.Dot(rel, canvasRight) / canvasW);
                    ny = Mathf.Clamp01(Vector3.Dot(rel, canvasUp) / canvasH);
                }
            }

            // 화면 가장자리에서 화살표가 밖으로 나가지 않도록 접근 평면 방향 자동 선택
            Vector2 planeDir2D;
            if (nx > 0.76f)
            {
                // 우측 패널(Execute, Active 카드 등): 화면 안쪽에서 바깥 버튼으로 가리켜 꼬리가 화면 내부에 머무름
                planeDir2D = ny > 0.5f ? new Vector2(0.84f, 0.54f) : new Vector2(0.84f, -0.54f);
            }
            else if (ny < 0.16f)
            {
                // 하단 크롬 바 아이콘(Process Library 등): 위에서 아래로 가리킴
                planeDir2D = nx < 0.35f ? new Vector2(-0.50f, -0.86f) : new Vector2(0.50f, -0.86f);
            }
            else if (ny > 0.85f)
            {
                // 상단 인접 버튼: 아래에서 위로 가리킴
                planeDir2D = new Vector2(-0.62f, 0.78f);
            }
            else
            {
                // 기본(트리/체크박스/링크): 우상단에서 좌하단으로 가리켜 본문 가림 최소화
                planeDir2D = new Vector2(-0.76f, -0.65f);
            }
            planeDir2D.Normalize();

            Vector3 inPlaneDir = (canvasRight * planeDir2D.x + canvasUp * planeDir2D.y).normalized;
            float tiltRad = mTiltFromScreenDeg * Mathf.Deg2Rad;
            mPointDirection = (inPlaneDir * Mathf.Cos(tiltRad) - outwardNormal * Mathf.Sin(tiltRad)).normalized;
            mUpDirection = outwardNormal;

            // 버튼 사각형 테두리 접점 계산 (버튼 텍스트·체크박스를 가리지 않도록 가장자리 외부에 끝점 배치)
            float halfW = Mathf.Abs(Vector3.Dot(mTargetCorners[3] - mTargetCorners[0], canvasRight)) * 0.5f;
            float halfH = Mathf.Abs(Vector3.Dot(mTargetCorners[1] - mTargetCorners[0], canvasUp)) * 0.5f;
            float tx = Mathf.Abs(planeDir2D.x) > 1e-4f ? halfW / Mathf.Abs(planeDir2D.x) : float.MaxValue;
            float ty = Mathf.Abs(planeDir2D.y) > 1e-4f ? halfH / Mathf.Abs(planeDir2D.y) : float.MaxValue;
            float edgeDist = Mathf.Min(tx, ty);
            if (float.IsInfinity(edgeDist) || float.IsNaN(edgeDist) || edgeDist > 0.25f) edgeDist = 0f;

            Vector3 edgeWorldPoint = btnCenter - inPlaneDir * edgeDist;
            mBaseTipWorldPos = edgeWorldPoint + outwardNormal * mScreenForwardOffsetMeters - mPointDirection * mEdgeGapMeters;
            mHasValidPlacement = true;
        }

        private void applyMaterialProperties()
        {
            if (mMeshRenderer == null || mMeshRenderer.sharedMaterial == null) return;
            Material mat = mMeshRenderer.sharedMaterial;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mArrowColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", mArrowColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", mArrowColor * mEmissionIntensity);
            }
        }

        /// <summary>A안(입체 8각 각면 클래식 화살표) 프로시저럴 3D 메쉬를 생성합니다. 끝점=(0,0,0), 꼬리=(0,0,-1).</summary>
        public static Mesh buildFacetedClassicArrowMesh()
        {
            const int sides = 8;
            const float tipZ = 0f;
            const float headBaseZ = -0.44f;
            const float headRadius = 0.21f;
            const float shaftRadiusFront = 0.092f;
            const float shaftRadiusRear = 0.106f;
            const float shaftRearZ = -0.93f;
            const float tailCapZ = -1.00f;
            const float tailCapRadius = 0.064f;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();

            Vector3[] headRing = new Vector3[sides];
            Vector3[] shaftFrontRing = new Vector3[sides];
            Vector3[] shaftRearRing = new Vector3[sides];
            Vector3[] tailCapRing = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float ang = (i / (float)sides) * Mathf.PI * 2f + Mathf.PI / sides;
                float c = Mathf.Cos(ang);
                float s = Mathf.Sin(ang);
                headRing[i] = new Vector3(c * headRadius, s * headRadius, headBaseZ);
                shaftFrontRing[i] = new Vector3(c * shaftRadiusFront, s * shaftRadiusFront, headBaseZ);
                shaftRearRing[i] = new Vector3(c * shaftRadiusRear, s * shaftRadiusRear, shaftRearZ);
                tailCapRing[i] = new Vector3(c * tailCapRadius, s * tailCapRadius, tailCapZ);
            }

            Vector3 tip = new Vector3(0f, 0f, tipZ);
            Vector3 tailCenter = new Vector3(0f, 0f, tailCapZ);

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;

                // 1. 화살촉 8각 피라미드 각면 (Unity 왼손 좌표계 외향 와인딩)
                addFlatTri(verts, norms, tris, tip, headRing[i], headRing[next]);

                // 2. 화살촉 뒷면 링(Collar)
                addFlatQuad(verts, norms, tris, headRing[i], shaftFrontRing[i], shaftFrontRing[next], headRing[next]);

                // 3. 샤프트 각면 (약한 테이퍼)
                addFlatQuad(verts, norms, tris, shaftFrontRing[i], shaftRearRing[i], shaftRearRing[next], shaftFrontRing[next]);

                // 4. 꼬리 챔퍼 링
                addFlatQuad(verts, norms, tris, shaftRearRing[i], tailCapRing[i], tailCapRing[next], shaftRearRing[next]);

                // 5. 꼬리 끝면 캡
                addFlatTri(verts, norms, tris, tailCenter, tailCapRing[next], tailCapRing[i]);
            }

            var mesh = new Mesh { name = "MESH_ScenarioGuideArrow3D" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void addFlatTri(List<Vector3> verts, List<Vector3> norms, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int idx = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            norms.Add(n); norms.Add(n); norms.Add(n);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        }

        private static void addFlatQuad(List<Vector3> verts, List<Vector3> norms, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int idx = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
        }

        /// <summary>URP Lit 기반 골드 발광 머티리얼을 생성합니다.</summary>
        public static Material createArrowMaterial(Color arrowColor, float emissionIntensity = 1.35f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard")
                            ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;

            var mat = new Material(shader) { name = "MAT_ScenarioGuideArrow3D" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", arrowColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", arrowColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.25f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.62f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", arrowColor * emissionIntensity);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return mat;
        }
    }
}
