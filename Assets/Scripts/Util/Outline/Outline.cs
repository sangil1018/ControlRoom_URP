//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]

public class Outline : MonoBehaviour
{
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();
    // 같은 메쉬는 같은 SmoothNormal을 공유한다. (오브젝트가 달라도 Mesh가 같을때 같은 외곽선이 표현됨)

    public enum Mode
    {
        OutlineAll,
        OutlineVisible,
        OutlineHidden,
        OutlineAndSilhouette,
        SilhouetteOnly,
        Length
    }

    public Mode OutlineMode
    {
        get { return m_outlineMode; }
        set
        {
            m_outlineMode = value;
            m_needsUpdate = true;
        }
    }

    public Color OutlineColor
    {
        get { return m_outlineColor; }
        set
        {
            m_outlineColor = value;
            m_needsUpdate = true;
        }
    }

    public float OutlineWidth
    {
        get { return m_outlineWidth; }
        set
        {
            m_outlineWidth = value;
            m_needsUpdate = true;
        }
    }

    [Serializable]
    private class ListVector3
    {
        public List<Vector3> data;
    }

    [SerializeField]
    private Mode m_outlineMode;

    [SerializeField]
    private Color m_outlineColor = Color.white;

    [SerializeField, Range(0f, 10f)]
    private float m_outlineWidth = 2f;

    [Header("Optional")]

    [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
    + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
    private bool m_precomputeOutline;

    [SerializeField, HideInInspector]
    private List<Mesh> m_bakeKeys = new List<Mesh>();

    [SerializeField, HideInInspector]
    private List<ListVector3> m_bakeValues = new List<ListVector3>();

    private Renderer m_renderers;
    private Material m_outlineMaskMaterial;
    private Material m_outlineFillMaterial;

    private bool m_needsUpdate;

    [SerializeField]
    private bool m_useCalculatedSmoothNormal = false;

    private void Awake()
    {
        // Cache renderers
        m_renderers = GetComponent<Renderer>();

        // Instantiate outline materials
        m_outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Material/M_OutlineMask"));
        m_outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Material/M_OutlineFill"));

        m_outlineMaskMaterial.name = "OutlineMask (Instance)";
        m_outlineFillMaterial.name = "OutlineFill (Instance)";

        // Retrieve or generate smooth normals
        LoadSmoothNormals();

        // Apply material properties immediately
        m_needsUpdate = true;
    }

    private void OnEnable()
    {
        // Append outline shaders
        var _materials = m_renderers.sharedMaterials.ToList();

        _materials.Add(m_outlineMaskMaterial);
        _materials.Add(m_outlineFillMaterial);

        m_renderers.materials = _materials.ToArray();
    }

    private void OnValidate()
    {
        // Update material properties
        m_needsUpdate = true;

        // Hash에서 Remove해야 변수가 변경되었을 때 SmoothNormals()를 제대로 호출할 수 있다.
        
        var _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter != null)
            registeredMeshes.Remove(_meshFilter.sharedMesh);

        var _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (_skinnedMeshRenderer != null)
            registeredMeshes.Remove(_skinnedMeshRenderer.sharedMesh);


        // Clear cache when baking is disabled or corrupted
        if (!m_precomputeOutline && m_bakeKeys.Count != 0 || m_bakeKeys.Count != m_bakeValues.Count)
        {
            m_bakeKeys.Clear();
            m_bakeValues.Clear();
        }

        // Generate smooth normals when baking is enabled
        if (m_precomputeOutline && m_bakeKeys.Count == 0)
            Bake();
    }

    private void Update()
    {
        if (m_needsUpdate)
        {
            m_needsUpdate = false;
            UpdateMaterialProperties();
        }
    }

    private void OnDisable()
    {
        // Remove outline shaders
        var _materials = m_renderers.sharedMaterials.ToList();

        _materials.Remove(m_outlineMaskMaterial);
        _materials.Remove(m_outlineFillMaterial);

        m_renderers.materials = _materials.ToArray();
    }

    private void OnDestroy()
    {
        // Destroy material instances
        Destroy(m_outlineMaskMaterial);
        Destroy(m_outlineFillMaterial);
    }

    private void Bake()
    {
        // Generate smooth normals for each mesh
        var _bakedMeshes = new HashSet<Mesh>();

        var _meshFilter = GetComponent<MeshFilter>();

        // Skip duplicates
        if (_meshFilter != null)
            _bakedMeshes.Add(_meshFilter.sharedMesh);

        // Serialize smooth normals
        var _smoothNormals = SmoothNormals(_meshFilter.sharedMesh);

        m_bakeKeys.Add(_meshFilter.sharedMesh);
        m_bakeValues.Add(new ListVector3() { data = _smoothNormals });
    }

    private void LoadSmoothNormals()
    {
        // Retrieve or generate smooth normals
        for (int i = 0; i < 1; i++)
        {
            var _meshFilter = GetComponent<MeshFilter>();

            if (_meshFilter == null) continue;

            // Skip if smooth normals have already been adopted
            if (!registeredMeshes.Add(_meshFilter.sharedMesh)) continue;

            // Retrieve or generate smooth normals
            var _index = m_bakeKeys.IndexOf(_meshFilter.sharedMesh);
            var _smoothNormals = (_index >= 0) ? m_bakeValues[_index].data : SmoothNormals(_meshFilter.sharedMesh);

            // Store smooth normals in UV3
            _meshFilter.sharedMesh.SetUVs(3, _smoothNormals);

            // Combine submeshes
            var _renderer = _meshFilter.GetComponent<Renderer>();

            if (_renderer != null)
                CombineSubmeshes(_meshFilter.sharedMesh, _renderer.sharedMaterials);
        }

        // Clear UV3 on skinned mesh renderers
        for (int i = 0; i < 1; i++)
        {
            var _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

            if (_skinnedMeshRenderer == null) continue;

            // Skip if UV3 has already been reset
            if (!registeredMeshes.Add(_skinnedMeshRenderer.sharedMesh)) continue;

            // Retrieve or generate smooth normals
            var _index = m_bakeKeys.IndexOf(_skinnedMeshRenderer.sharedMesh);
            var _smoothNormals = (_index >= 0) ? m_bakeValues[_index].data : SmoothNormals(_skinnedMeshRenderer.sharedMesh);

            // Clear UV3
            // Store smooth normals in UV3
            _skinnedMeshRenderer.sharedMesh.SetUVs(3, _smoothNormals);

            // Combine submeshes
            CombineSubmeshes(_skinnedMeshRenderer.sharedMesh, _skinnedMeshRenderer.sharedMaterials);
        }

        /*
        // Skip if smooth normals have already been adopted
        if (!registeredMeshes.Add(_meshFilter.sharedMesh))
            continue;

        // Retrieve or generate smooth normals
        var _index = m_bakeKeys.IndexOf(_meshFilter.sharedMesh);
        var _smoothNormals = (_index >= 0) ? m_bakeValues[_index].data : SmoothNormals(_meshFilter.sharedMesh);

        // Store smooth normals in UV3
        _meshFilter.sharedMesh.SetUVs(3, _smoothNormals);

        // Combine submeshes
        var _renderer = _meshFilter.GetComponent<Renderer>();

        if (_renderer != null)
            CombineSubmeshes(_meshFilter.sharedMesh, _renderer.sharedMaterials);
    }

    // Clear UV3 on skinned mesh renderers
    foreach (var _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>())
    {

        // Skip if UV3 has already been reset
        if (!registeredMeshes.Add(_skinnedMeshRenderer.sharedMesh))
            continue;

        // Retrieve or generate smooth normals
        var _index = m_bakeKeys.IndexOf(_skinnedMeshRenderer.sharedMesh);
        var _smoothNormals = (_index >= 0) ? m_bakeValues[_index].data : SmoothNormals(_skinnedMeshRenderer.sharedMesh);

        // Clear UV3
        // Store smooth normals in UV3
        _skinnedMeshRenderer.sharedMesh.SetUVs(3, _smoothNormals);

        // Combine submeshes
        CombineSubmeshes(_skinnedMeshRenderer.sharedMesh, _skinnedMeshRenderer.sharedMaterials);
*/
    }

    private List<Vector3> SmoothNormals(Mesh _mesh)
    {
        // 이 함수에서 새로 계산된 노멀은 일부 메쉬에서 이상한 외곽선을 생성할 수 있음
        if (!m_useCalculatedSmoothNormal) return new List<Vector3>(_mesh.normals);
        // Group vertices by location
        var _groups = _mesh.vertices.Select((_vertex, _index) => new KeyValuePair<Vector3, int>(_vertex, _index)).GroupBy(_pair => _pair.Key);

        // Copy normals to a new list
        var _smoothNormals = new List<Vector3>(_mesh.normals);

        // Average normals for grouped vertices
        foreach (var _group in _groups)
        {
            // Skip single vertices
            if (_group.Count() == 1)
                continue;

            // Calculate the average normal
            var _smoothNormal = Vector3.zero;

            foreach (var pair in _group)
                _smoothNormal += _smoothNormals[pair.Value];

            _smoothNormal.Normalize();

            // Assign smooth normal to each vertex
            foreach (var _pair in _group)
                _smoothNormals[_pair.Value] = _smoothNormal;
        }

        return _smoothNormals;
    }

    private void CombineSubmeshes(Mesh _mesh, Material[] _materials)
    {
        // Skip meshes with a single submesh
        if (_mesh.subMeshCount == 1)
        {
            return;
        }

        // Skip if submesh count exceeds material count
        if (_mesh.subMeshCount > _materials.Length)
        {
            return;
        }

        // Append combined submesh
        _mesh.subMeshCount++;
        _mesh.SetTriangles(_mesh.triangles, _mesh.subMeshCount - 1);
    }

    private void UpdateMaterialProperties()
    {
        // Apply properties according to mode
        m_outlineFillMaterial.SetColor("_OutlineColor", m_outlineColor);

        switch (m_outlineMode)
        {
            case Mode.OutlineAll:
                m_outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                m_outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                m_outlineFillMaterial.SetFloat("_OutlineWidth", m_outlineWidth);
                break;

            case Mode.OutlineVisible:
                m_outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                m_outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                m_outlineFillMaterial.SetFloat("_OutlineWidth", m_outlineWidth);
                break;

            case Mode.OutlineHidden:
                m_outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                m_outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                m_outlineFillMaterial.SetFloat("_OutlineWidth", m_outlineWidth);
                break;

            case Mode.OutlineAndSilhouette:
                m_outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                m_outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                m_outlineFillMaterial.SetFloat("_OutlineWidth", m_outlineWidth);
                break;

            case Mode.SilhouetteOnly:
                m_outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                m_outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                m_outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
                break;
        }
    }
}
