using UnityEditor;
using TMPro.EditorUtilities;


[CustomEditor(typeof(LocalizedTextMeshUGUI), true)]
[CanEditMultipleObjects]
public class LocalizedTextMeshUGUIEditor : TMP_EditorPanelUI
{
    private SerializedProperty stringTableIndexProp;

    protected override void OnEnable()
    {
        base.OnEnable();
        stringTableIndexProp = serializedObject.FindProperty("stringTableIndex");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 인스펙터 최상단에 문자열 테이블 인덱스 필드 표시
        EditorGUILayout.PropertyField(stringTableIndexProp);
        EditorGUILayout.Space(10);

        serializedObject.ApplyModifiedProperties();

        // 기존 TextMeshProUGUI의 기본 인스펙터 UI 그리기
        base.OnInspectorGUI();
    }
}
