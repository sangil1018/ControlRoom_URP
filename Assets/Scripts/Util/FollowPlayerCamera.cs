using UnityEngine;

public class FollowPlayerCamera : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform m_target;
    [SerializeField] private Vector3 m_offset = Vector3.zero;

    private void Start()
    {
        if (m_target == null && Camera.main != null)
            m_target = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (m_target != null)
        {
            Vector3 targetPosition = m_target.position +
                                m_target.TransformDirection(m_offset);

            transform.SetPositionAndRotation(targetPosition, m_target.rotation);
        }       
    }
}
